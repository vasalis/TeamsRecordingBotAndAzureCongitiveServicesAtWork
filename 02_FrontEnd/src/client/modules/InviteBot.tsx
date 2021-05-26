import * as React from "react";
import * as PropTypes from "prop-types";
import { LanguageEntity } from "../Models/ModelEntities";
import { Flex } from "@fluentui/react-northstar";
import LanguageSelection from "./LanguageSelection";
import { useState, useEffect } from "react";

const InviteBot = (props) =>
{
    const [currentTranscriptionLang, setcurrentTranscriptionLang] = useState<string>();    
    const [currentTranslationLang, setcurrentTranslationLang] = useState<string>();    

    const inviteBot = () => {

        if(currentTranscriptionLang && currentTranslationLang)
        {
            let lEndPoint = process.env.REACT_APP_BACKEND_API as string;
            lEndPoint = lEndPoint + "api/InviteBot";
            
            let lBody = {
                            JoinURL: props.currentJoinUrl ? encodeURI(props.currentJoinUrl) : "",
                            TranscriptionLanguage: currentTranscriptionLang,
                            TranslationLanguages: currentTranslationLang
                        };        
    
            console.log("Got invite bot endpoint: " + lEndPoint + ". Body is: " + JSON.stringify(lBody));
    
            fetch(lEndPoint, {
                method: "POST",
                headers: {
                    Accept: "application/json",
                    "Content-Type": "application/json"
                },
                body: JSON.stringify(lBody)
            })
                .then(response => response.json())
                .then(data => {
                    console.log("Got response after adding bot: " + JSON.stringify(data) + " -> Call id is: " + data.callId);
                    props.setcurrentCallId(data.callId);
                }).catch(function(error) {
                    console.log(error);
                });
        }        
    };

    const myLanguages: LanguageEntity[] = [        
        { key: 'en-US', text: 'English' },
        { key: 'el-GR', text: 'Greek' }        
      ];

    return (
        <Flex column fill={true}>
            <LanguageSelection                
                setLangSelection={setcurrentTranscriptionLang}
                languages = {myLanguages}
                label="Transcription language"
            />
            <LanguageSelection          
                setLangSelection={setcurrentTranslationLang}      
                languages = {myLanguages}
                label="Translation language"
            />

            <button
                onClick={inviteBot}>Invite  Bot</button>
        </Flex>
        
    );
};

InviteBot.propTypes = {
    currentJoinUrl: PropTypes.string,
    setcurrentCallId: PropTypes.func    
};

export default InviteBot;