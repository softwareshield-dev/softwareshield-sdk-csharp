# softwareshield-sdk-csharp

SoftwareShield SDK for C#

****************************

   GameShield V5 SDK/C#


****************************

GS5.cs:   The glue code for C#
          You can copy it to your own project to compile as part of your product.


GS5.dll:  The pre-built assembly of GS5.cs as following:

          csc /t:library /r:Newtonsoft.Json.dll GS5.cs


          You can add reference to it in your .NET project and do not need the GS5.cs.


Newtonsoft.Json.dll: Popular high-performance JSON framework for .NET
	  
	  https://www.nuget.org/packages/Newtonsoft.Json/


	  It is needed only when:
	 
	(1) you want to activate your product in-app manually via SDK (not wrapped). 
          If your product will be wrapped by SoftwareShield IDE, it won't be needed since
          the activation of wrapped product is implemented in HTML/javascript. 

	(2) you prefer using .Net than native code (implemented in gsCore/SDK5.3);
            If you want to use .Net implementation of CheckPoint Activation, you must comment out line (in gs5.cs):

		#define ONLINE_ACTIVATION_DOTNET

	Since SDK 5.3 we have fully implemented the activation apis in native code, the .Net implementation might be 
     deprecated later.
