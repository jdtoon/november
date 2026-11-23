import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { StandaloneHost } from "@november/shared";
import "@november/ui";
import App from "./App";

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <StandaloneHost name="Incoming payments">{(props) => <App {...props} />}</StandaloneHost>
  </StrictMode>,
);
