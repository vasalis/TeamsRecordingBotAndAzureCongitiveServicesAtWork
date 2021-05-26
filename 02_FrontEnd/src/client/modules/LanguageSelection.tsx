import * as React from "react";
import * as PropTypes from "prop-types";
import { Dropdown, IDropdownOption } from "@fluentui/react";
import { LanguageEntity } from "../Models/ModelEntities";

const LanguageSelection = (props) =>
{  
    const langChanged = (event: React.FormEvent<HTMLDivElement>, option?: IDropdownOption<LanguageEntity>, index?: number) => {
        if (option) {            
            props.setLangSelection(option.key);
        }
    };

    return (
        
        <Dropdown            
            onChange={langChanged}
            options={props.languages}
            placeholder={props.label}
        />
    );
};

LanguageSelection.propTypes = {
    label: PropTypes.string,
    setLangSelection: PropTypes.func,
    languages: PropTypes.arrayOf(PropTypes.instanceOf(LanguageEntity))
};

export default LanguageSelection;