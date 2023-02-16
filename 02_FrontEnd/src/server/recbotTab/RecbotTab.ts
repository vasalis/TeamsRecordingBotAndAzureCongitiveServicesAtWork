import { PreventIframe } from "express-msteams-host";

/**
 * Used as place holder for the decorators
 */
@PreventIframe("/recbotTab/index.html")
@PreventIframe("/recbotTab/config.html")
@PreventIframe("/recbotTab/remove.html")
export class RecbotTab {
}
