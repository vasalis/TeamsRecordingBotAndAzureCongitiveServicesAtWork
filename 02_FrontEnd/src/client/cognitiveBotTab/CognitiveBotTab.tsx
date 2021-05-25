import * as React from "react";
import { Flex, Provider, Text, Button } from "@fluentui/react-northstar";
import {
    initializeIcons, IDropdownOption
} from "@fluentui/react";
import { useState, useEffect } from "react";
import { useTeams } from "msteams-react-base-component";
import * as microsoftTeams from "@microsoft/teams-js";
import MyCalls from "../modules/MyCalls";
import { TranscriptionEntity, CallEntity } from "../Models/ModelEntities";
import MyTranscriptions from "../modules/MyTranscriptions";
import { AppInsightsContext } from "@microsoft/applicationinsights-react-js";
import { reactPlugin } from "../modules/AppInsights";
import InviteBot from "../modules/InviteBot";
import jwtDecode from "jwt-decode";
import { Providers, TeamsProvider } from "@microsoft/mgt";

/**
 * Implementation of the Cognitive Bot content page
 */
export const CognitiveBotTab = () => {

    const [{ inTeams, theme, context }] = useTeams();
    const [myActiveCalls, setActiveCalls] = useState<CallEntity[]>();
    const [currentCallId, setcurrentCallId] = useState<string>();
    const [myTranscriptions, setMyTranscriptions] = useState<TranscriptionEntity[]>();
    const [inMeeting, setinMeeting] = useState<boolean>();
    const [currentMeetingId, setcurrentMeetingId] = useState<string>();

    const [name, setName] = useState<string>();
    const [error, setError] = useState<string>();

    const callIdChanged = (event: React.FormEvent<HTMLDivElement>, option?: IDropdownOption<CallEntity>, index?: number) => {
        if (option) {
            const lCall = option as unknown as CallEntity;
            setcurrentCallId(lCall.callid);
        }
    };

    useEffect(() => {
        if (inTeams === true) {
            microsoftTeams.authentication.getAuthToken({
                successCallback: (token: string) => {
                    const decoded: { [key: string]: any; } = jwtDecode(token) as { [key: string]: any; };
                    setName(decoded!.name);
                    microsoftTeams.appInitialization.notifySuccess();

                    if(context && context.meetingId)
                    {
                        console.log("In Teams Meeting, meeting id is: " + context.meetingId);
                        console.log("Context is: " + JSON.stringify(context));
                        setinMeeting(true);
                        setcurrentMeetingId(context.meetingId);
                    }
                },
                failureCallback: (message: string) => {
                    setError(message);
                    microsoftTeams.appInitialization.notifyFailure({
                        reason: microsoftTeams.appInitialization.FailedReason.AuthFailed,
                        message
                    });
                },
                resources: [process.env.SSO_APP_URI as string]
            });
        }        
    }, [inTeams]);

    const inviteBot = () => {
        let lEndPoint = process.env.REACT_APP_BACKEND_API as string;
        lEndPoint = lEndPoint + "api/InviteBot";
        console.log("Got invite bot endpoint: " + lEndPoint);

        fetch(lEndPoint, {
            method: "POST",
            headers: {
                Accept: "application/json",
                "Content-Type": "application/json"
            },
            body: currentMeetingId
        })
            .then(response => response.text())
            .then(data => {
                setcurrentCallId(data);
            }).catch(function(error) {
                console.log(error);
            });
    };

    const getAuthAndGetMeetingInfo = () => {
        let lAppRegId = process.env.SSO_APP_ID as string;

        console.log("App id: " + lAppRegId);

        const lTeamsConfig = {
            clientId: lAppRegId,
            authPopupUrl: window.location.origin + "/authpopup/",
            scopes: ["User.Read.All", "Group.Read.All", "OnlineMeetings.Read"]
        };

        TeamsProvider.microsoftTeamsLib = microsoftTeams;
        const lTe = new TeamsProvider(lTeamsConfig);
        Providers.globalProvider = lTe;

        lTe.login().then(getMeetingInfoFromGraph);
    }

    const getMeetingInfoFromGraph = () => {
        let provider = Providers.globalProvider;
        let graphClient = provider.graph.client;
        var lResult = graphClient.api("/me/onlineMeetings/MCMxOTptZWV0aW5nX1lUQXdORE00TW1VdE5qVmlPQzAwTnpReExXRmhZVGN0TWpKaE1qWXdabVl4WW1KbEB0aHJlYWQudjIjMA==").get();
                                                          

        alert("Meeting info: " + JSON.stringify(lResult));
    }

    useEffect(() => {
        let lEndPoint = process.env.REACT_APP_BACKEND_API as string;
        lEndPoint = lEndPoint + "api/GetActiveCalls";
        console.log("Got calls endpoint: " + lEndPoint);

        fetch(lEndPoint, {
            method: "GET",
            headers: {
                Accept: "application/json",
                "Content-Type": "application/json"
            }
        })
            .then(response => response.json())
            .then(data => {
                setActiveCalls(data);
            }).catch(function(error) {
                console.log(error);
            });
    }, []);

    useEffect(() => {
        const interval = setInterval(() => {
            if (currentCallId) {
                let lEndPoint = process.env.REACT_APP_BACKEND_API as string;
                lEndPoint = lEndPoint + "api/GetTranscriptions";
                console.log("Got calls endpoint: " + lEndPoint);

                fetch(lEndPoint, {
                    method: "POST",
                    headers: {
                        Accept: "application/json",
                        "Content-Type": "application/json"
                    },
                    body: currentCallId
                })
                    .then(response => response.json())
                    .then(data => {
                        setMyTranscriptions(data);
                    }).catch(function(error) {
                        console.log(error);
                    });
            }
        }, 500);
        return () => clearInterval(interval);
    }, [currentCallId]);

    /**
     * The render() method to create the UI of the tab
     */
    return (
        <AppInsightsContext.Provider value={reactPlugin}>
            <Provider theme={theme}>
                <Flex column fill={true}>

                    <Flex.Item>
                        <div>
                            <div>
                                <Text content={`Hello 3 ${name}`} />
                            </div>
                            {error && <div><Text content={`An SSO error occurred ${error}`} /></div>}

                            <div>
                                <Button onClick={getAuthAndGetMeetingInfo}>A sample button</Button>
                            </div>
                        </div>
                    </Flex.Item>

                    {!inMeeting ? 
                        (<MyCalls calls={myActiveCalls} onChange={callIdChanged}/>) :
                        (<InviteBot onClick={inviteBot} />) 
                    }                             
                    
                    <MyTranscriptions transcriptions={myTranscriptions} />
                </Flex>
            </Provider>
        </AppInsightsContext.Provider>        
    );
};
