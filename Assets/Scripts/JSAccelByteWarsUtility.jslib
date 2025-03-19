// Copyright (c) 2025 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

var LibraryAccelByteWarsUtility = 
{
    JSCopyToClipboard: function(textPtr) 
    {
        // Use UTF8ToString to convert the pointer to a JavaScript string
        var text = UTF8ToString(textPtr);

        // Create a temporary textarea element to copy the text
        var textarea = document.createElement('textarea');
        document.body.appendChild(textarea);
        textarea.value = text;
        textarea.select();
        document.execCommand('copy');
        document.body.removeChild(textarea);
    }
};

mergeInto(LibraryManager.library, LibraryAccelByteWarsUtility);