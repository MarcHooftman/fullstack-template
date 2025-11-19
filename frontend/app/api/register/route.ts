import { NextRequest } from 'next/server';

const API_PROXY_TARGET = process.env.API_PROXY_TARGET || 'http://backend:80';

async function proxyRegister(req: NextRequest) {
    const destBase = API_PROXY_TARGET.replace(/\/$/, '');
    const destUrl = `${destBase}/api/auth/register`;

    // Forward headers except host
    const forwarded = new Headers(req.headers as any);
    forwarded.delete('host');

    // Preserve raw body
    let body: ArrayBuffer | undefined = undefined;
    try {
        const buf = await req.arrayBuffer();
        if (buf && buf.byteLength > 0) body = buf;
    } catch (e) {
        // ignore
    }

    const res = await fetch(destUrl, {
        method: 'POST',
        headers: forwarded,
        body,
        redirect: 'manual',
    });

    const responseHeaders = new Headers(res.headers);
    responseHeaders.delete('transfer-encoding');

    const responseBuffer = await res.arrayBuffer();

    return new Response(responseBuffer, {
        status: res.status,
        headers: responseHeaders,
    });
}

export async function POST(req: NextRequest, context: any) {
    return proxyRegister(req);
}

export const dynamic = 'force-dynamic';
