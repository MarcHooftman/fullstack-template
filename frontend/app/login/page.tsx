import LoginForm from "@/components/auth/LoginForm";

export default function LoginPage() {
    return (
        <div className="container mx-auto p-8 max-w-md">
            <h1 className="text-2xl font-bold mb-4">Login</h1>
            <div className="shadow rounded p-6">
                <LoginForm redirectTo="/" />
            </div>
        </div>
    );
}
