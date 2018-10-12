# Serverless Slack Queue

This is a Web API project meant to be deployed to AWS as a serverless app. It can then receive [Slack](https://www.slack.com) [slash commands](https://api.slack.com/slash-commands) and send the requests to SQS queues for further processing. This allows the slash commands to return quickly to Slack, notifying the user that their request was received, while queueing the request for processing by one or more workers that will read the data from the queues.

To configure the project, enter the following information in appsettings.json:
* Set _SigningSecret_ to the secret key you have shared with Slack in order to verify that the requests [actually come from Slack](https://api.slack.com/docs/verifying-requests-from-slack).
* Set _SqsUrlPrefix_ to the value that has the region you want to deploy to and your account id. SQS urls are of the form https://sqs.{region}.amazonaws.com/{account}/.

Once the project is deployed, it waits for incoming commands from Slack. Once a command is received, the application tries to send the message body to a queue called "slack_{command}" where {command} is the shorthand of the Slack slash command. If the queue is not found, the application will try to create a queue and notify the user to resend the command. 

Before deploying the application make sure to go through _serverless.template_ and validate that the parameters are to your liking. Points to note are the reserved concurrent executions set to 10, the timeout set to 10 seconds, the memory size set to 1024 MBs and the policies used. If you want to limit the policy scope, you can create an appropriate role and assign it to the _Role_ entry just above _Policies_. Note that the application needs to have enough rights to send messages to SQS and also create new queues.

The easiest way to deploy the application is to use the [AWS Toolkit for Visual Studio](https://aws.amazon.com/visualstudio/).
