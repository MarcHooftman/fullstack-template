import SignedIn from "@/components/auth/SignedIn";
import SignedOut from "@/components/auth/SignedOut";
import Image from "next/image";
import Link from "next/link";

export default function Home() {
  return (
    <>
      <header>
        <div className="container mx-auto flex flex-col items-center justify-center p-4">
          <h1 className="text-4xl font-bold mb-4">Welcome to the Fullstack Template!</h1>
          <p className="text-lg mb-8 text-center">
            This is a starter template for building fullstack applications with Next.js and TypeScript.
          </p>
          <Image
            src="/logo.png"
            alt="Fullstack Template Logo"
            width={200}
            height={200}
          />
        </div>
      </header>
      <main>
        <SignedIn>
          <div className="container mx-auto p-4">
            <h2 className="text-2xl font-semibold mb-4">You are signed in!</h2>
            <p className="text-base">
              Explore the features of this template and start building your application.
            </p>
          </div>
        </SignedIn>
        <SignedOut>
          <div className="container mx-auto p-4">
            <h2 className="text-2xl font-semibold mb-4">You are signed out!</h2>
            <p className="text-base">
              Please sign in to access more features and start building your application.
            </p>
            <div className="mt-4 flex gap-3">
              <Link href="/login" className="inline-block rounded bg-blue-600 text-white px-4 py-2 hover:bg-blue-700">Login</Link>
              <Link href="/register" className="inline-block rounded border border-blue-600 text-blue-600 px-4 py-2 hover:bg-blue-50">Register</Link>
            </div>
          </div>
        </SignedOut>
      </main>
    </>
  );
}
