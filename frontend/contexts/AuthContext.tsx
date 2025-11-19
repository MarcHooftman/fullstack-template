"use client";

import { AuthContextValue } from "@/types/auth";
import { createContext } from "react";

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export default AuthContext;
