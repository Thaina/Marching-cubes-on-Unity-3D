mergeInto(LibraryManager.library, {
    PointerLocked: function() {
        return document.pointerLockElement != null;
    },

    LocalStorageSave : function(key, data) {
        localStorage.setItem(UTF8ToString(key), UTF8ToString(data));
    },

    LocalStorageLoad : function(key) {
        var returnStr = localStorage.getItem(UTF8ToString(key));
        if(returnStr == null)
            return null;

        var bufferSize = lengthBytesUTF8(returnStr) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(returnStr, buffer, bufferSize);
        return buffer;
    },

    LocalStorageDelete : function(key) {
        localStorage.removeItem(UTF8ToString(key));
    },
    
    LocalStorageClear : function() {
        localStorage.clear();
    },
    
    LocalStorageLength : function() {
        return localStorage.length;
    },
    
    LocalStorageKey : function(index) {
        var returnStr = localStorage.key(index);
        if(returnStr == null)
            return null;

        var bufferSize = lengthBytesUTF8(returnStr) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(returnStr, buffer, bufferSize);
        return buffer;
    },
});