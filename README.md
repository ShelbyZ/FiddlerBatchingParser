# FiddlerBatchingParser
Fiddler Request/Response inspector that can handle multipart/batching

# Purpose
Fiddler did not offer support to format embedded content types like those found inside of multipart/* messages.  Being able to view the inner Json response in a batched message makes it a bit easier to debug things like the UCWA's event channel.  Any data that isn't Json should be represented as a raw (string) interpretation of the data.

# Samples
Find 3 sample traces in the 'Samples' folder.

# UCWA
http://ucwa.lync.com/

# UCWA & Batching-Related Content
* http://blogs.claritycon.com/ucpractice/2013/08/06/ucwa-deciphering-multipartbatching-fiddler/
* http://blogs.claritycon.com/ucpractice/2015/04/26/ucwa-numbers-6-batch/

# Build a Custom Inspector
http://docs.telerik.com/fiddler/Extend-Fiddler/CustomInspector

# Dependencies
* Fiddler v2.4.9.0 - http://www.telerik.com/fiddler
* Json.Net v6.0.3.17227 - https://json.codeplex.com/
* JSON Viewer v1.0.0.0 - https://jsonviewer.codeplex.com/
