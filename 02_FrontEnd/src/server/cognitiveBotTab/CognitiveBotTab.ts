import { PreventIframe } from "express-msteams-host";

/**
 * Used as place holder for the decorators
 */
@PreventIframe("/cognitiveBotTab/index.html")
@PreventIframe("/cognitiveBotTab/config.html")
@PreventIframe("/cognitiveBotTab/remove.html")
export class CognitiveBotTab {
}
