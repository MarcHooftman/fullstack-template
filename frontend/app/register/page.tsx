import RegisterForm from "@/components/auth/RegisterForm";

export default function RegisterPage() {
    return (
        <div className="container mx-auto p-8 max-w-md">
            <h1 className="text-2xl font-bold mb-4">Register</h1>
            <div className="shadow rounded p-6">
                <RegisterForm />
            </div>
        </div>
    );
}
