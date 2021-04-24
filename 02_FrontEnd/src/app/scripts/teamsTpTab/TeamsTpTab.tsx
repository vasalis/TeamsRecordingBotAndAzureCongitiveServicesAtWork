import * as React from "react";
import {
    PrimaryButton,
    TeamsThemeContext,
    getContext
} from "msteams-ui-components-react";
import TeamsBaseComponent, { ITeamsBaseComponentProps, ITeamsBaseComponentState } from "msteams-react-base-component";
import * as microsoftTeams from "@microsoft/teams-js";
import { Providers, TeamsProvider } from "@microsoft/mgt";
import {
    Stack, IStackStyles, IStackTokens, Persona, PersonaSize,
    initializeIcons, IIconProps, IconButton, getTheme, mergeStyleSets, DefaultPalette, OverflowSet, IButtonStyles,
    FontWeights, Modal, DetailsList, DetailsListLayoutMode, IColumn, DefaultButton, SelectionMode, Link, TextField,
    IOverflowSetItemProps,
    StackItem,
    Label,
    IStackItemStyles, Dropdown, IDropdownOption, SelectableOptionMenuItemType
} from "@fluentui/react";

// Tokens definition
const sectionStackTokens: IStackTokens = { childrenGap: 10 };
const wrapStackTokens: IStackTokens = { childrenGap: 5 };

/**
 * State for the teamsTpTabTab React component
 */
export interface ITeamsTpTabState extends ITeamsBaseComponentState {
    currentStatus: string;    
    activeCallItems: CallEntity[];
    activeCallDDItems: IDropdownOption[];
    transcriptionItems: TranscriptionEntity[];
    isCallIdSet: boolean;
    fetchState: string;
    callIdToFetch: string
}

/**
 * Properties for the teamsTpTabTab React component
 */
export interface ITeamsTpTabProps extends ITeamsBaseComponentProps {

}

class TranscriptionEntity {
    public id: string;
    public who: string;
    public text: string;
    public translations: string;
    public when: string;
}

class CallEntity implements IDropdownOption {
    isSelected?: boolean | undefined;
    key: string | number;
    id?: string | undefined;
    text: string;
    title?: string | undefined;
    itemType?: SelectableOptionMenuItemType | undefined;
    index?: number | undefined;
    ariaLabel?: string | undefined;
    selected?: boolean | undefined;
    disabled?: boolean | undefined;
    hidden?: boolean | undefined;
    data?: any;
        
    //public callid: string;    
    //public text: string;    
    public when: string;    

    public set callid(value) {
        this.key = value;
    }

    public get callid() {
        return this.key;
    }    
}

/**
 * Implementation of the Teams_TP Tab content page
 */
export class TeamsTpTab extends TeamsBaseComponent<ITeamsTpTabProps, ITeamsTpTabState> {
    
    private _selection: Selection;
    private intervalForActiveCalls;
    private interval;

    public componentWillMount() {
        this.updateTheme(this.getQueryVariable("theme"));
        this.setState({
            fontSize: this.pageFontSize()
        });

        if (this.inTeams()) {
            microsoftTeams.initialize();
            microsoftTeams.registerOnThemeChangeHandler(this.updateTheme);

            initializeIcons();      
            
        }
    }

    public componentWillUnmount() {
        if(this.interval)
        {
            clearInterval(this.interval);
        }        
      }          

    private RenderAddCallId() {
        const addIcon: IIconProps = { iconName: 'Add' };
        return (
            <Stack horizontal horizontalAlign="end">
                <Dropdown
                    placeholder="Select an active call"
                    label="My active calls"
                    options={this.state.activeCallItems}                    
                />
                <TextField onChange={this._onChangeText} />
                <Link onClick={() => this.GetTranscriptionsForUI()}>
                    <Persona imageInitials="+"
                        initialsColor="green" text="Start" secondaryText={this.state.fetchState} size={PersonaSize.size40} />                    
                </Link>
                <Link onClick={() => this.StopTranscriptionsForUI()}>                    
                    <Persona imageInitials="X"
                    initialsColor="green" text="Stop" secondaryText={this.state.fetchState} size={PersonaSize.size40} />
                </Link>
            </Stack>
        );
    }

    private async StopTranscriptionsForUI() {
        this.setState({
            isCallIdSet: false
        });  
        
        this.setState({
            fetchState: "Stopping..."
        });   
    }

    private async GetActiveCallsForUI() {       

        this.intervalForActiveCalls = setInterval(() => this.GetActiveCallsFromApi(), 2000);
    }

    private async GetTranscriptionsForUI() {
        this.setState({
            isCallIdSet: true
        });


        this.interval = setInterval(() => this.GetDataPeriodically(), 1000);
    }
    
    private async GetDataPeriodically() {
        if(this.state.isCallIdSet)
        {
            this.setState({
                fetchState: "Fetching data"
            });

            await this.GetTranscriptionsFromApi(this.state.callIdToFetch);
        }
        else
        {
            clearInterval(this.interval);

            this.setState({
                fetchState: "Stoped!"
            });   
        }
    }

    private _onChangeText = (ev: React.FormEvent<HTMLInputElement | HTMLTextAreaElement>, text: string): void => {
        this.setState({
            callIdToFetch: text
        });        
    };    

    private RenderCallTranscription() {
        if (this.state.transcriptionItems) {
            const context = getContext({
                baseFontSize: this.state.fontSize,
                style: this.state.theme
            });
            const { rem, font } = context;
            const { sizes, weights } = font;
            const styles = {
                header: { ...sizes.caption, ...weights.semibold },
                section: { ...sizes.base, marginTop: rem(1.4), marginBottom: rem(1.4) },
                footer: { ...sizes.xsmall }
            };

            // Non-mutating styles definition
            const itemStyles: IStackItemStyles = {
                root: {
                  background: DefaultPalette.white,                  
                  padding: 5,
                  width: '90%'
                },
              };

            return this.state.transcriptionItems.map((team, index) => {

                return (
                    <Stack styles={itemStyles}>
                        <Persona text={team.who} secondaryText={new Date(Date.parse(team.when)).toLocaleString()}  size={PersonaSize.size24} />    
                        <Label>{team.text}</Label>
                        <Label><b>Translation:</b>&nbsp; {team.translations}</Label>                        
                    </Stack>    
                );
            });
        }
    }
    

    // Util Functions -> move to another place
    // TODO: Replace with the End point of the back end, now set from env 
    private async GetTranscriptionsFromApi(aCallId: string) {
        try {            
            var lEndPoint = process.env.REACT_APP_BACKEND_API as string;
            lEndPoint = lEndPoint + "api/GetTranscriptions";
            console.log('Got transcriptions endpoint: ' + lEndPoint);
            return fetch(lEndPoint, {
                method: 'POST',
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json',
                },
                body: aCallId
                })
                .then(response => response.json())
                .then(data => {
                    this.setState({ transcriptionItems: data });
                    this.setState({
                        fetchState: ""
                    });
                }).catch(function(error) {                    
                    console.log(error);
                });
            
        } catch (error) {
            this.Log("GetTranscriptions error: " + JSON.stringify(error));
        }
    }

    // Util Functions -> move to another place
    // TODO: Replace with the End point of the back end, now set from env 
    private async GetActiveCallsFromApi() {
        try {            
            var lEndPoint = process.env.REACT_APP_BACKEND_API as string;
            lEndPoint = lEndPoint + "api/GetActiveCalls";

            console.log('Got calls endpoint: ' + lEndPoint);
            return fetch(lEndPoint, {
                method: 'GET',
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json',
                }                
                })
                .then(response => response.json())
                .then(data => {
                    this.setState({ activeCallItems: data });                   
                }).catch(function(error) {                    
                    console.log(error);
                });
            
        } catch (error) {
            this.Log("GetActiveCalls error: " + JSON.stringify(error));
        }
    }

    private Log(aMsg: string) {
        //TODO: add app insights
        console.log(aMsg);
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

        const stackStyleDesktop: IStackStyles = {
            root: {
                overflow: "visible"
            },
        };

        this.GetActiveCallsForUI();

        return (
            <TeamsThemeContext.Provider value={context}>
                {this.RenderAddCallId()}                
                <Stack horizontal wrap styles={stackStyleDesktop} tokens={wrapStackTokens}>
                    {this.RenderCallTranscription()}
                </Stack>
            </TeamsThemeContext.Provider>
        );
    }
}
