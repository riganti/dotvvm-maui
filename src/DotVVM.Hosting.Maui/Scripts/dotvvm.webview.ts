/// <reference path="../node_modules/dotvvm-types/DotVVM.d.ts" />

declare var webviewCompileConstants: { platform: "windows" | "android" };
type ReceivedMessage = { messageId: number } &
    (
        { action: "GetViewModelSnapshot" } |
        { action: "PatchViewModel", patch: any } | 
        { action: "CallNamedCommand", elementSelector: string, commandName: string, args: any[] } | 
        { action: "HttpRequest", body: string, headers: [{ Key: string, Value: string }], status: number }
    );

if ("webview" in dotvvm) {
    throw new Error("DotVVM WebView extension is already registered!");
}
(dotvvm as any).webview = (function () {
    const pendingRequests: { resolve: (result: any) => void, reject: (result: any) => void }[] = [];

    // notify about new route
    dotvvm.events.initCompleted.subscribe(notifyNavigationCompleted);
    if (dotvvm.events.spaNavigated) {
        dotvvm.events.spaNavigated.subscribe(notifyNavigationCompleted);
    }

    // handle commands from the webview
    (window.external as any).receiveMessage(async (json: any) => {

        function handleCommand(message: ReceivedMessage) {
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

            } else if (webviewCompileConstants.platform === "android" && message.action === "HttpRequest") {
                const promise = pendingRequests[message.messageId]

                const headers = new Headers();
                for (const h of message.headers) {
                    headers.append(h.Key, h.Value);
                }
                const response = new Response(message.body, { headers, status: message.status });
                promise.resolve(response);
                return;

            } else {
                throw new Error(`Command not found!`);
            }
        }

        const message = <ReceivedMessage>JSON.parse(json);
        try {
            const result = await handleCommand(message);
            if (typeof result !== "undefined") { 
                sendMessage({
                    type: "HandlerCommand",
                    id: message.messageId,
                    result: JSON.stringify(result)
                });
            }
        }
        catch (err) {
            sendMessage({
                type: "HandlerCommand",
                id: message.messageId,
                errorMessage: JSON.stringify(err)
            });
        }
    });

    function sendMessage(message: any) {
        (window.external as any).sendMessage(message);
    }

    async function sendMessageAndWaitForResponse<T>(message: any): Promise<T> {
        message.id = pendingRequests.length;
        const promise = new Promise<T>((resolve, reject) => {
            pendingRequests[message.id] = { resolve, reject };
            sendMessage(message);
        });
        return await promise;
    }
    
    function notifyNavigationCompleted() {
        sendMessage({
            type: "NavigationCompleted",
            routeName: (dotvvm as any).routeName        // TODO
        });
    }

    function sendPageNotification(methodName: string, args: any[]) {
        sendMessage({
            type: "PageNotification",
            methodName: methodName,
            args: args
        });
    }

    if (webviewCompileConstants.platform === "android") {
        // use customFetch instead of fetch for POST requests
        async function externalFetch(url: string, init: RequestInit): Promise<Response> {
            if (init.method?.toUpperCase() === "GET") {
                return await fetch(url, init);
            }

            const headers: any = {};
            (<Headers>init.headers)?.forEach((v, k) => headers[k] = v);

            return await sendMessageAndWaitForResponse<Response>({
                type: "HttpRequest",
                url,
                method: init.method || "GET",
                headers: headers,
                body: init.body as string
            });
        }
        (dotvvm as any).customFetch = externalFetch;
    }

    return {
        sendNotification: sendPageNotification
    };
})();