dotvvm.webview = dotvvm.webview || (function () {
    window.external.receiveMessage(message => {
        // handler commands
        if (message.action == "PatchViewModel") {
            const result = dotvvm.viewModules.call(message.elementSelector, message.commandName, message.args, true);
            sendMessage({
                type: "handlerCommand",
                messageId: message.messageId,
                result: Promise.resolve(result)
            });
        }
        if (message.action == "GetViewModelSnapshot") {
            sendMessage({
                type: "handlerCommand",
                messageId: message.messageId,
                result: JSON.stringify(dotvvm.state)
            });
        } else if (message.action == "PatchViewModel") {
            dotvvm.patchViewModel(message.patch);
            sendMessage({
                type: "handlerCommand",
                messageId: message.messageId,
                result: true
            });
        }
    });

    function sendMessage(message) {
        window.external.sendMessage(message);
    }
    
    return {
        notifyNavigationCompleted(routeName) {
            sendMessage({
                type: "navigationCompleted",
                routeName: routeName
            });
        }
    }
})();