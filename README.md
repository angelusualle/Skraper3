# Skraper3
Skraper 3 Application. Monitors websites for changes and alerts users through SMS and Email.

## Set up
As per appsettings.json, ensure a AWSConfig.json file exists with AWS credentials:

`
{
    "AWSSecretKey":"xxxxxxxxxxxxxxxxxxxx",
    "AWSAccessKey":"AKIxxxxxxxxxxxxxxxxx"
}
`

A valid subscriptions.json, used as the input to the service (location configurable in appsettings.json) is needed, example of contents:

[{"Email":"youremail@somewhere.com","MobileNumber":"11234567890","URL":"http://webtomonitor55.com/"}]

