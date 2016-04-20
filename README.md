# kinect_2_server
C# server streaming features of the Kinect 2 such as speech &amp; gesture recognition, skeleton tracking and original images

First, be sure that you have Windows 8 or later versions otherwise you won't be able to install the SDK of Kinect2.

You have to download and install these following features and SDKs :

Kinect 2 SDK :
https://www.microsoft.com/en-us/download/details.aspx?id=44561

Microsoft Speech platform SDK :
https://www.microsoft.com/en-us/download/details.aspx?id=27226

Media feature pack for N and KN version of Windows 8 :
https://www.microsoft.com/en-us/download/details.aspx?id=30685

Media feature pack for N and KN version of Windows 10 :
https://www.microsoft.com/en-us/download/details.aspx?id=48231

Json.NET :
Install-Package Newtonsoft.Json in NuGet
then add "using Newtonsoft.Json;" in your project

ZeroMQ :
Install-Package clrzmq -Version 2.2.5 in NuGet
then add "using ZeroMQ;" in your project
