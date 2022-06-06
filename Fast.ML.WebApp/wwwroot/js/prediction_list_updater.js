"use strict";

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

startConnection().then(
    () => hubConnection.on("ReceivePredictions", 
        (message, error) => {
        if (error) {
            console.log(message);
            let params = (new URL(window.location)).searchParams;
            let taskId = params.get("task_id");
            let host = window.location.host;
            let queryParams = "upload_file_for_prediction?algorithm=" + 
                message + "&prefix=" + taskId + "&success=false";
            let url = new URL("https://" + host + "/" + queryParams);
            window.location.href = url.toString();
        } else {
            console.log(message);
            let tr = document.getElementById("prediction").cloneNode(true);
            tr.hidden = false;
            let predictionLink = tr.querySelector("#prediction_link");
            predictionLink.setAttribute(
                "href", 
                predictionLink.href + "&algorithm=" + message.toLowerCase());
            let algorithmName = tr.querySelector("#algorithm_name");
            algorithmName.innerHTML = message;
            document.getElementById("predictionsTable")
                .hidden = false;
            document.getElementById("predictionsTable")
                .tBodies[0].appendChild(tr);
        }
    })
);

hubConnection.onreconnecting((e) => 
    console.log("reconnecting", e));
hubConnection.onreconnected((e) => 
    console.log("reconnected", e));
      