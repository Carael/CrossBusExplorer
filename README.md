# Cross Bus Explorer

Cross Bus Explorer is a Cross Platform Service Bus Explorer (Windows, McOS and Linux)

## Features

- Create, List, Edit, Delete, Disable of Queues, Topics, Subscriptions
- Subscription rules management (create, edit, update)
- Purge messages
- Requeue messages
- Send messages
- Resend edited messages

![chrome_SbCnkRVg9b](https://user-images.githubusercontent.com/13761704/207661917-7f1ea66f-3878-4e88-a771-def3b5c52088.gif)

## Known issues
### Linux snap installer

The snap package is not signed, to install it run following command:
snap install cross-bus-explorer-0.2.0.snap --dangerous
![image](https://user-images.githubusercontent.com/6861396/206653698-f1146dad-7d03-4f72-ae0a-c5d55cc3ee6d.png)


### Mac Os

The app is not signed so it's blocked by default. Please go to Privacy and Security and click 'Open anyway' on the option in 'Security' section. Also above the 'Allow applications downloaded from' option need to be set to: 'App Store and identified developers'.
![image](https://user-images.githubusercontent.com/6861396/206653511-2584d668-f3fa-47f2-acd4-6242788b9996.png)
