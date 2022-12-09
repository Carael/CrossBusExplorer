# Cross Bus Explorer

Cross Bus Explorer is a Cross Platform Service Bus Explorer (Windows, McOS and Linux)

## Features

- Create, List, Edit, Delete, Disable of Queues, Topics, Subscriptions
- Subscription rules management (create, edit, update)
- Purge messages
- Requeue messages
- Send messages
- Resend edited messages

![image](https://user-images.githubusercontent.com/6861396/204493783-9a98340f-5d00-4c67-848f-25b7c6df5c69.png)

## Known issues
### Linux snap installer

The snap package is not signed, to install it run following command:
snap install cross-bus-explorer-0.2.0.snap --dangerous
![image](https://user-images.githubusercontent.com/6861396/206653698-f1146dad-7d03-4f72-ae0a-c5d55cc3ee6d.png)


### Mac Os

The app is not signed so it's blocked by default. Please go to Privacy and Security and click 'Open anyway' on the option in 'Security' section. Also above the 'Allow applications downloaded from' option need to be set to: 'App Store and identified developers'.
![image](https://user-images.githubusercontent.com/6861396/206653511-2584d668-f3fa-47f2-acd4-6242788b9996.png)
