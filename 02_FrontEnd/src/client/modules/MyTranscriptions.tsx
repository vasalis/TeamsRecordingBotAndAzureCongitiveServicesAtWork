import * as React from "react";
import { Flex } from "@fluentui/react-northstar";
import MyTranscription from "./MyTranscription";
import * as PropTypes from "prop-types";
import { TranscriptionEntity } from "../Models/ModelEntities";
import { useState, useEffect } from "react";

const MyTranscriptions = (props) =>
{
    const [myTranscriptions, setMyTranscriptions] = useState<TranscriptionEntity[]>();

    useEffect(() => {
        const interval = setInterval(() => {
            if (props.currentCallId) {
                let lEndPoint = process.env.REACT_APP_BACKEND_API as string;
                lEndPoint = lEndPoint + "api/GetTranscriptions";
                console.log("Got calls endpoint: " + lEndPoint);

                fetch(lEndPoint, {
                    method: "POST",
                    headers: {
                        Accept: "application/json",
                        "Content-Type": "application/json"
                    },
                    body: props.currentCallId
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
    }, [props.currentCallId]);

    return (
        <Flex column gap="gap.small" styles={{
            padding: ".4rem"
        }}>
            { myTranscriptions?.map((team, index) => (
                <MyTranscription key={team.id} transcription={team} />
            ))}
        </Flex>
    );
};


MyTranscriptions.propTypes = {
    currentCallId: PropTypes.string    
};

export default MyTranscriptions;
