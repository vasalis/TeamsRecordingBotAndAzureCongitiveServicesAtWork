import * as React from 'react';
import { Flex } from "@fluentui/react-northstar";
import MyTranscription from './MyTranscription';

const MyTranscriptions = (props) => (
    <Flex column gap="gap.small" styles={{
        padding: ".8rem"
    }}>
        { props.transcriptions?.map((team, index) => (
            <MyTranscription transcription={team} />
        ))}
    </Flex>
);

export default MyTranscriptions;
