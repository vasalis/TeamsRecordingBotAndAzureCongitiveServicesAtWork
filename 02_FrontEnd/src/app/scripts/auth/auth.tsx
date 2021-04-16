import * as React from "react";
import {
    PrimaryButton,
    TeamsThemeContext,
    Panel,
    PanelBody,
    PanelHeader,
    PanelFooter,
    Surface,
    getContext
} from "msteams-ui-components-react";
import TeamsBaseComponent, { ITeamsBaseComponentProps, ITeamsBaseComponentState } from "msteams-react-base-component";
import * as microsoftTeams from "@microsoft/teams-js";
import {Providers, TeamsProvider} from '@microsoft/mgt';

/**
 * State for the teamsTpTabTab React component
 */
export interface IAuthState extends ITeamsBaseComponentState {
    entityId?: string;
}

/**
 * Properties for the teamsTpTabTab React component
 */
export interface IAuthProps extends ITeamsBaseComponentProps {

}

/**
 * Implementation of the Teams_TP Tab content page
 */
export class Auth extends TeamsBaseComponent<IAuthProps, IAuthState> {

    public componentWillMount() {
        this.updateTheme(this.getQueryVariable("theme"));
        this.setState({
            fontSize: this.pageFontSize()
        });

        this.setState({
            entityId: "Auth page"
        });      

        microsoftTeams.initialize();
        TeamsProvider.microsoftTeamsLib = microsoftTeams;
        TeamsProvider.handleAuth();
    }

    /**
     * The render() method to create the UI of the tab
     */
    public render() {
        const context = getContext({
            baseFontSize: this.state.fontSize,
            style: this.state.theme
        });
        const { rem, font } = context;
        const { sizes, weights } = font;
        const styles = {
            header: { ...sizes.title, ...weights.semibold },
            section: { ...sizes.base, marginTop: rem(1.4), marginBottom: rem(1.4) },
            footer: { ...sizes.xsmall }
        };
        return (
            <TeamsThemeContext.Provider value={context}>
                <Surface>
                    <Panel>
                        <PanelHeader>
                            <div style={styles.header}>Authentication in progress</div>
                        </PanelHeader>                       
                    </Panel>
                </Surface>
            </TeamsThemeContext.Provider>
        );
    }
}
