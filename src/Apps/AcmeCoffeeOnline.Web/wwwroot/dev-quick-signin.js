export async function signIn(email, secret, roles) {
    let url = `/test-auth/seeded-session?email=${encodeURIComponent(email)}`;
    if (roles) {
        url += `&roles=${encodeURIComponent(roles)}`;
    }

    const response = await fetch(url, {
        method: 'GET',
        headers: { 'X-Test-Auth-Secret': secret },
        credentials: 'include'
    });

    if (!response.ok) {
        throw new Error(`Seeded sign-in failed with status ${response.status}`);
    }

    location.href = '/';
}
