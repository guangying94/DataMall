# Road Traffic Images

There are 2 section in this project, which is 
1. Traffic Images visualization on Power BI
1. Custom Vision on Traffic Images

## Traffic Images Visualization on Power BI
Please refer to this [**link**](https://www.youtube.com/watch?v=ZzZ4Q9QEFaA) for the full tutorial.

## Custom Vision on Traffic Images
I'm leveraging Microsoft Cognitive Services - [**Custom Vision**](https://www.customvision.ai/) for this section. Essentially, the main idea is to train a model that can recognize the traffic condition, such as light traffic or heavy traffic.

Before we upload and tag the images, I have written an Azure Function to store all traffic images in Blob Storage. Refer to the _Azure Function_ folder above.
