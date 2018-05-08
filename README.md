# ResourceHealth-To-LogAnalytics

NOTE:
This code was written by someone else, but it was part of a project I was working on, so it is included here.

The logic apps I created is called “healthmonitor” and I sent the data to the Log Analytics called “proshuskyautomationoms”. I did not deploy the function apps to Azure, I only ran them locally, but, I did create a functions app called “resourcehealthmonitor”. There is a Storage Account I created called “resourcehealthmac70”, which contains two storage queues (resourcegroup-healthmonitor & resource-healthmonitor).
 
The current Flow is: First azure function (called “HealthOfSubscription”) checks for all Resource groups in a subscription and sends a message to a queue (resourcegroup-healthmonitor) with the information about each resource group. Another function (called “HealthOfResourceGroup”) picks up the message and checks the health of all of the resources within each resource group (i.e. one function execution per resource group). If it discovers failures, it writes those errors to another queue (resource-healthmonitor). These error messages are picked up by Logic Apps and sent to Log Analytics and also to send grid (for email notification).
