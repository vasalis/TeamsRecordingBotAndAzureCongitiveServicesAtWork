import * as React from "react";
import { Flex } from "@fluentui/react-northstar";
import MyTranscription from "./MyTranscription";
import * as PropTypes from "prop-types";
import { TranscriptionEntity } from "../Models/ModelEntities";

const MyTranscriptions = (props) => (
    <Flex column gap="gap.small" styles={{
        padding: ".8rem"
    }}>
        { props.transcriptions?.map((team, index) => (
            <MyTranscription key={team.id} transcription={team} />
        ))}
    </Flex>
);

MyTranscriptions.propTypes = {
    transcriptions: PropTypes.arrayOf(PropTypes.instanceOf(TranscriptionEntity))
};

export default MyTranscriptions;
