import * as React from "react";
import { Flex, Provider } from "@fluentui/react-northstar";
import { useState, useEffect } from "react";
import { useTeams } from "msteams-react-base-component";
import * as microsoftTeams from "@microsoft/teams-js";
import MyCalls from "../modules/MyCalls";
import MyTranscriptions from "../modules/MyTranscriptions";
import { AppInsightsContext } from "@microsoft/applicationinsights-react-js";
import { reactPlugin } from "../modules/AppInsights";
import InviteBot from "../modules/InviteBot";

/**
 * Implementation of the Cognitive Bot content page
 */
export const CognitiveBotTab = () => {

    const [{ inTeams, theme, context }] = useTeams();    
    const [currentCallId, setcurrentCallId] = useState<string>();    
    const [inMeeting, setinMeeting] = useState<boolean>();
    const [currentJoinUrl, setcurrentJoinUrl] = useState<string>();    

    useEffect(() => {
        if (inTeams === true) {
            microsoftTeams.appInitialization.notifySuccess();

            // Get Join Meeting Url
            if(context && context.meetingId && context.chatId && context.tid && context.userObjectId)
            {
                console.log("In Teams Meeting, meeting id is: " + context.meetingId);
                console.log("Context is: " + JSON.stringify(context));
                setinMeeting(true);

                // Try to create Join Url
                let lJoinUrl = "https://teams.microsoft.com/l/meetup-join/CHAT_ID/0?context={\"Tid\":\"T_ID\",\"Oid\":\"O_ID\"}".
                replace("CHAT_ID", context.chatId).
                replace("T_ID", context.tid).
                replace("O_ID", context.userObjectId);

                console.log("Join web url is: " + lJoinUrl);

                setcurrentJoinUrl(lJoinUrl);
            }

        }
    }, [inTeams]);     

    /**
     * The render() method to create the UI of the tab
     */
    return (
        <AppInsightsContext.Provider value={reactPlugin}>
            <Provider theme={theme}>
                <Flex column fill={true}>

                    {inMeeting && !currentCallId ?                        
                        ( <InviteBot currentJoinUrl={currentJoinUrl} setcurrentCallId={setcurrentCallId}  />) :
                        (null)
                    }

                    {!inMeeting ? 
                        (<MyCalls setcurrentCallId={setcurrentCallId} />) :
                        (null)
                    }

                    <MyTranscriptions currentCallId={currentCallId} />
                </Flex>
            </Provider>
        </AppInsightsContext.Provider>        
    );
};
