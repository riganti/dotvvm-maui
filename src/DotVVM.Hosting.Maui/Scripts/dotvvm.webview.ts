/// <reference path="../../../dotvvm/src/Framework/Framework/obj/typescript-types/dotvvm.d.ts" />

type ReceivedMessage = (
        { action: "GetViewModelSnapshot" } |
        { action: "PatchViewModel", patch: any } | 
        { action: "CallNamedCommand", elementSelector: string, commandName: string, args: any[] }
    );

const webview = dotvvm.webview!;

dotvvm.events.initCompleted.subscribe(() => {
    webview.registerMessageProcessor((untypedMessage: any) => {

        const message = <ReceivedMessage>untypedMessage;
        if (message.action === "GetViewModelSnapshot") {
            const snapshot = { ...dotvvm.state };
            delete snapshot.$csrfToken;
            return snapshot;

        } else if (message.action === "PatchViewModel") {
            dotvvm.patchState(message.patch);
            return null;

        } else if (message.action === "CallNamedCommand") {
            // TODO
            const element = message.elementSelector;
            return (dotvvm.viewModules as any).callNamedCommand(element, message.commandName, message.args, true);
        }
    });

    // export public API
    dotvvm.webview = { ...dotvvm.webview!, sendPageNotification } as any;

    // notify about new route when the page changes
    notifyNavigationCompleted();
    dotvvm.events.spaNavigated!.subscribe(notifyNavigationCompleted);
});

function notifyNavigationCompleted() {
    webview.sendMessage({
        type: "NavigationCompleted",
        routeName: dotvvm.routeName
    });
}

function sendPageNotification(methodName: string, args: any[]) {
    webview.sendMessage({
        type: "PageNotification",
        methodName: methodName,
        args: args
    });
}
