import * as React from 'react';
import { Dropdown} from "@fluentui/react";

const MyCalls = (props) => (
  <Dropdown
    onChange={props.onChange}
    options={props.calls}
    placeholder="Select a call" 
  />
);

export default MyCalls;