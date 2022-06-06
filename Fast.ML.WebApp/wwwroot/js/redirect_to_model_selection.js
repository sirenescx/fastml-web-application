"use strict";

// jQuery.support.cors = true;

const hubConnection = new signalR.HubConnectionBuilder()
    // .withUrl("https://0.0.0.0:7131/pageUpdateHub")
    .withUrl("https://fastmlapi.imet-db.ru/pageUpdateHub")
    .withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Trace)
    .build();

const startConnection = async () => {
    try {
        await hubConnection.start();
        console.log("connected");
    } catch (e) {
        console.log(e);
    }
};

startConnection().then(() => hubConnection.on(
    "ReceiveTrainEnd", (message) => {
    console.log(message)
    let url = window.location.href.replace("task", "model");
    url = new URL("selectmodels?task_id=" + message, url)
    window.location.href = url.toString();
}));


hubConnection.onreconnecting((e) => 
    console.log("reconnecting", e));
hubConnection.onreconnected((e) => 
    console.log("reconnected", e));
      