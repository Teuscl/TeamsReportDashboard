import {JwtPayload} from "jwt-decode";

interface JwtUser extends JwtPayload{
    unique_name: string;
    role: string;
    email: string;
    nameid: string;
}

export default JwtUser;