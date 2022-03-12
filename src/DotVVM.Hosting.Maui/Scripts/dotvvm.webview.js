dotvvm.webview = dotvvm.webview || (function () {
    dotvvm.events.initCompleted.subscribe(notifyNavigationCompleted);
    if (dotvvm.events.spaNavigated) {
        dotvvm.events.spaNavigated.subscribe(notifyNavigationCompleted);
    }

    window.external.receiveMessage(async message => {
        // handler commands
        message = JSON.parse(message);

        const commands = {            
            "GetViewModelSnapshot": () => {
                const snapshot = { ...dotvvm.state };
                delete snapshot.$csrfToken;
                return snapshot;
            },
            "PatchViewModel": () => {
                dotvvm.patchState(message.patch);
                return null;
            },
            "CallNamedCommand": () => {
                const element = message.elementSelector;
                //const element = document.querySelector(message.elementSelector);
                //if (!element) {
                //    throw `Element with selector '${message.elementSelector}' not found!`;
                //}
                return dotvvm.viewModules.callNamedCommand(element, message.commandName, message.args, true);
            }
        }
        const command = commands[message.action];
        if (command) {
            try {
                const result = await command();
                sendMessage({
                    type: "handlerCommand",
                    messageId: message.messageId,
                    result: JSON.stringify(result)
                });
            }
            catch (err) {
                sendMessage({
                    type: "handlerCommand",
                    messageId: message.messageId,
                    errorMessage: JSON.stringify(err)
                });
            }
        } else {
            sendMessage({
                type: "handlerCommand",
                messageId: message.messageId,
                errorMessage: "Command not found!"
            });
        }        
    });

    function sendMessage(message) {
        window.external.sendMessage(message);
    }
    
    function notifyNavigationCompleted() {
        sendMessage({
            type: "navigationCompleted",
            routeName: dotvvm.routeName
        });
    }

    function sendPageNotification(methodName, args) {
        sendMessage({
            type: "pageNotification",
            methodName: methodName,
            args: args
        });
    }

    return {
        sendNotification: sendPageNotification
    };
})();