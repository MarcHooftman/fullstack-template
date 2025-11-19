"use client";

import type { ReactNode } from "react";
import useAuth from "@/hooks/useAuth";

export const SignedOut = ({ children }: { children?: ReactNode }) => {
    const { user } = useAuth();
    if (user) return null;
    return <>{children}</>;
};

export default SignedOut;
