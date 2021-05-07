import * as React from "react";
import { Dropdown } from "@fluentui/react";
import * as PropTypes from "prop-types";
import { CallEntity } from "../Models/ModelEntities";

const MyCalls = (props) => (
    <Dropdown
        onChange={props.onChange}
        options={props.calls}
        placeholder="Select a call"
    />
);

MyCalls.propTypes = {
    onChange: PropTypes.func,
    calls: PropTypes.arrayOf(PropTypes.instanceOf(CallEntity))
};

export default MyCalls;
