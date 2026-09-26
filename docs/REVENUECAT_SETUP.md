# RevenueCat setup

The game uses RevenueCat Purchases Unity SDK 9.11.1. Its optional supporter
purchase keeps all gameplay free and grants a cosmetic warm community glow.

## Dashboard configuration

1. Create a RevenueCat project for **The Littles: Edmonton School Run**.
2. Add a Test Store app.
3. Create a non-subscription product with identifier `support_edmonton`.
4. Create an entitlement with identifier `supporter`.
5. Attach the product to the entitlement.
6. Add the product as a package in the Current offering.
7. Copy the public Test Store API key into
   `Assets/Resources/RevenueCatPublicKey.txt` in the private build project.

The key file is ignored by Git. The code fetches the Current offering, starts
the purchase only after a user taps the button, checks the `supporter`
entitlement, restores purchases on request, and activates the cosmetic reward.

## Release safety

The Test Store key is for development and hackathon demonstration only. Replace
it with the platform-specific public Android SDK key before uploading a
production build to Google Play. Never use a RevenueCat secret key in Unity.
