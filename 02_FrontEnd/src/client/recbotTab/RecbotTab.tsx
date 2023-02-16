import * as React from "react";
import { Provider, Flex, Text, Button, Header } from "@fluentui/react-northstar";
import { useState, useEffect } from "react";
import { useTeams } from "msteams-react-base-component";
import { app } from "@microsoft/teams-js";
import MyCalls from "../modules/MyCalls";
import MyTranscriptions from "../modules/MyTranscriptions";
import InviteBot from "../modules/InviteBot";

/**
 * Implementation of the recbot content page
 */
export const RecbotTab = () => {

    const [{ inTeams, theme, context }] = useTeams();    
    const [currentCallId, setcurrentCallId] = useState<string>();    
    const [inMeeting, setinMeeting] = useState<boolean>();
    const [currentJoinUrl, setcurrentJoinUrl] = useState<string>();    

    useEffect(() => {
        if (inTeams === true) {
            app.notifySuccess();

            // Get Join Meeting Url
            if(context && context.meeting?.id && context.chat?.id && context.team?.internalId && context.user?.id)
            {
                console.log("In Teams Meeting, meeting id is: " + context.meeting?.id);
                console.log("Context is: " + JSON.stringify(context));
                setinMeeting(true);

                // Try to create Join Url
                let lJoinUrl = "https://teams.microsoft.com/l/meetup-join/CHAT_ID/0?context={\"Tid\":\"T_ID\",\"Oid\":\"O_ID\"}".
                replace("CHAT_ID", context.chat?.id).
                replace("T_ID", context.team?.internalId).
                replace("O_ID", context.user?.id);

                console.log("Join web url is: " + lJoinUrl);

                setcurrentJoinUrl(lJoinUrl);
            }

        }
    }, [inTeams]);     

    /**
     * The render() method to create the UI of the tab
     */
    return (
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
    );
};
