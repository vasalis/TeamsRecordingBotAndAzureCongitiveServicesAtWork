import * as React from "react";
import * as PropTypes from "prop-types";

const InviteBot = (props) => (
    <button
        onClick={props.onClick}>Invite Cog Bot</button>
);

InviteBot.propTypes = {
    onClick: PropTypes.func    
};

export default InviteBot;