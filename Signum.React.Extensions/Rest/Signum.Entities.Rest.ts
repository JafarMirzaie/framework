//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Entities from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Basics from '../../../Framework/Signum.React/Scripts/Signum.Entities.Basics'
import * as Authorization from '../Authorization/Signum.Entities.Authorization'


export const QueryStringValueEmbedded = new Type<QueryStringValueEmbedded>("QueryStringValueEmbedded");
export interface QueryStringValueEmbedded extends Entities.EmbeddedEntity {
  Type: "QueryStringValueEmbedded";
  key: string;
  value: string;
}

export const RestApiKeyEntity = new Type<RestApiKeyEntity>("RestApiKey");
export interface RestApiKeyEntity extends Entities.Entity {
  Type: "RestApiKey";
  user: Entities.Lite<Authorization.UserEntity>;
  apiKey: string;
}

export module RestApiKeyOperation {
  export const Save : Entities.ExecuteSymbol<RestApiKeyEntity> = registerSymbol("Operation", "RestApiKeyOperation.Save");
  export const Delete : Entities.DeleteSymbol<RestApiKeyEntity> = registerSymbol("Operation", "RestApiKeyOperation.Delete");
}

export const RestLogEntity = new Type<RestLogEntity>("RestLog");
export interface RestLogEntity extends Entities.Entity {
  Type: "RestLog";
  httpMethod: string | null;
  url: string;
  startDate: string;
  endDate: string;
  replayDate: string | null;
  requestBody: string;
  queryString: Entities.MList<QueryStringValueEmbedded>;
  user: Entities.Lite<Basics.IUserEntity>;
  userHostAddress: string;
  userHostName: string;
  referrer: string;
  controller: string | null;
  controllerName: string;
  action: string;
  machineName: string;
  applicationName: string;
  exception: Entities.Lite<Basics.ExceptionEntity>;
  responseBody: string;
  replayState: RestLogReplayState | null;
  changedPercentage: number | null;
  allowReplay: boolean;
}

export const RestLogReplayState = new EnumType<RestLogReplayState>("RestLogReplayState");
export type RestLogReplayState =
  "NoChanges" |
  "WithChanges";


