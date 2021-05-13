import * as React from "react";
import { Flex, Provider } from "@fluentui/react-northstar";
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

/**
 * Implementation of the Cognitive Bot content page
 */
export const CognitiveBotTab = () => {

    const [{ inTeams, theme, context }] = useTeams();
    const [myActiveCalls, setActiveCalls] = useState<CallEntity[]>();
    const [currentCallId, setcurrentCallId] = useState<string>();
    const [myTranscriptions, setMyTranscriptions] = useState<TranscriptionEntity[]>();
    const [inMeeting, setinMeeting] = useState<boolean>();

    const callIdChanged = (event: React.FormEvent<HTMLDivElement>, option?: IDropdownOption<CallEntity>, index?: number) => {
        if (option) {
            const lCall = option as unknown as CallEntity;
            setcurrentCallId(lCall.callid);
        }
    };

    useEffect(() => {
        if (inTeams === true) {
            microsoftTeams.appInitialization.notifySuccess();

            console.log("my context: " + JSON.stringify(context));

            if(context && context.meetingId)
            {
                console.log("In Teams Meeting, meeting id is: " + context.meetingId);
                setinMeeting(true);
            }
        }
    }, [inTeams]);

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
                    {inMeeting ? (<MyCalls calls={myActiveCalls} onChange={callIdChanged}/>) :
                    (<InviteBot />) 
                    }                             
                    
                    <MyTranscriptions transcriptions={myTranscriptions} />
                </Flex>
            </Provider>
        </AppInsightsContext.Provider>        
    );
};
