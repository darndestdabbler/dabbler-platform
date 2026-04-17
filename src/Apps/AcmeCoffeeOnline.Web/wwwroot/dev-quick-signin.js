export async function signIn(email, secret) {
    const response = await fetch(
        `/test-auth/seeded-session?email=${encodeURIComponent(email)}`,
        {
            method: 'GET',
            headers: { 'X-Test-Auth-Secret': secret },
            credentials: 'include'
        }
    );

    if (!response.ok) {
        throw new Error(`Seeded sign-in failed with status ${response.status}`);
    }

    location.href = '/';
}
