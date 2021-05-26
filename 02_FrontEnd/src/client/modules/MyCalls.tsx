import * as React from "react";
import { Dropdown } from "@fluentui/react";
import * as PropTypes from "prop-types";
import { CallEntity } from "../Models/ModelEntities";
import { useState, useEffect } from "react";
import { IDropdownOption } from "@fluentui/react";

const MyCalls = (props) =>
{ 
    const [myActiveCalls, setActiveCalls] = useState<CallEntity[]>();    

    const callIdChanged = (event: React.FormEvent<HTMLDivElement>, option?: IDropdownOption<CallEntity>, index?: number) => {
        if (option) {
            const lCall = option as unknown as CallEntity;
            props.setcurrentCallId(lCall.callid);
        }
    };

    useEffect(() => {
        let lEndPoint = process.env.REACT_APP_BACKEND_API as string;
        lEndPoint = lEndPoint + "api/GetActiveCalls";
        console.log("Got calls endpoint: " + lEndPoint);

        fetch(lEndPoint, {
            method: "GET",
            headers: {
                Accept: "application/json",
                "Content-Type": "application/json"
            }
        })
            .then(response => response.json())
            .then(data => {
                setActiveCalls(data);
            }).catch(function(error) {
                console.log(error);
            });
    }, []);    

    return (
        <Dropdown
            onChange={callIdChanged}
            options={myActiveCalls ? myActiveCalls as unknown as IDropdownOption[] : []}
            placeholder="Select a call"
        />
    );
};

MyCalls.propTypes = {    
    setcurrentCallId: PropTypes.func
};

export default MyCalls;
