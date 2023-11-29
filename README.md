# HttpLoadGenerator

## Important notes

The configuration of the API endpoint and key is supposed to be set via the JSON configuration file.

### Observations

During development I came across a problem with the provided API endpoint. Even though CURL and Postman do work with such endpoint, clients based on System.Net.Http.HttpClient seem to be incompatible. I spent a lot of time trying to figure out what is wrong with it. The PowerShell cmdlet Invoke-RestMethod also does not work with your endpoint.

I suspect that HttpClient has troubles trying to negotiate a connection with your service behind the AWS API Gateway. It seems that HttpClient is not able to establish an HTTP/2 connection with TLS 1.2 (it goes with HTTP 1.1), leading to HTTP 400 Bad Request. I am not sure why this is happening, but I have seen a similar issue before when working with web services hosted in AWS ECS Clusters. On that occasion, I was able to fix it by downgrading the endpoint to HTTP1.

I was not willing to spend more time in the investigation of the root cause, because I believe this was not the purpose of the coding task. Also because trying to solve it without having access to AWS and the web service itself is like debugging with a hand tied on my back. For this reason, I set up a HTTP request interception in Fiddler application, so that my requests could be automatically responded. This worked like a fake web service, and allowed me to go forward with the project. The other aspects of my application do comply with the requirements.

It is possible that you do not have this same problem when you run my code in your development environment, specially if you run it on POSIX. But if you do come across the same issue, you can set up an AutoResponder in Fiddler, just like I did.