import * as React from "react";
import { Flex, Provider, Text, Button } from "@fluentui/react-northstar";
import { useState, useEffect } from "react";
import { useTeams } from "msteams-react-base-component";
import * as microsoftTeams from "@microsoft/teams-js";
import { AppInsightsContext } from "@microsoft/applicationinsights-react-js";
import { reactPlugin } from "../modules/AppInsights";
import { Providers, TeamsProvider } from "@microsoft/mgt";

/**
 * Implementation of the Cognitive Bot content page
 */
export const authPopUp = () => {

    const [{ inTeams, theme }] = useTeams();    

    useEffect(() => {
        console.log("About to handle auth...");
            microsoftTeams.initialize();
            TeamsProvider.microsoftTeamsLib = microsoftTeams;
            TeamsProvider.handleAuth();        
    }, []);    

    /**
     * The render() method to create the UI of the tab
     */
    return (
        <AppInsightsContext.Provider value={reactPlugin}>
            <Provider theme={theme}>
                <Flex column fill={true}>

                    <Flex.Item>
                        <div>
                        Authentication in progress
                        </div>
                    </Flex.Item>
                </Flex>
            </Provider>
        </AppInsightsContext.Provider>        
    );
};
