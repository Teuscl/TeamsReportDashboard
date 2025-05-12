import {JwtPayload} from "jwt-decode";

interface JwtUser extends JwtPayload{
    name: string;
    role: string;
    email: string;
    id: string;
}

export default JwtUser;