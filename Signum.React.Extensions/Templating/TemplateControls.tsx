﻿import * as React from 'react'
import { Tab, Tabs, ButtonToolbar }from 'react-bootstrap'
import { classes } from '../../../Framework/Signum.React/Scripts/Globals'
import { FormGroup, FormControlStatic, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityDetail, EntityList, EntityRepeater, EntityTabRepeater} from '../../../Framework/Signum.React/Scripts/Lines'
import { SubTokensOptions, QueryToken, QueryTokenType, hasAnyOrAll }  from '../../../Framework/Signum.React/Scripts/FindOptions'
import { SearchControl }  from '../../../Framework/Signum.React/Scripts/Search'
import { getToString, getMixin }  from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import { TypeContext, FormGroupStyle } from '../../../Framework/Signum.React/Scripts/TypeContext'
import { TemplateTokenMessage } from './Signum.Entities.Templating'

import QueryTokenEntityBuilder from '../UserAssets/Templates/QueryTokenEntityBuilder'
import QueryTokenBuilder from '../../../Framework/Signum.React/Scripts/SearchControl/QueryTokenBuilder'

export interface TemplateControlsProps {
    queryKey: string;
    onInsert: (newCode: string) => void;
    forHtml: boolean
}

export interface TemplateControlsState {
    currentToken: QueryToken
}

export default class TemplateControls extends React.Component<TemplateControlsProps, TemplateControlsState>{

    state = { currentToken: null } as TemplateControlsState;

    render() {
        var ct = this.state.currentToken;

        if (!this.props.queryKey)
            return null;

        return (
            <div className="form-sm">
                <QueryTokenBuilder queryToken={ct} queryKey={this.props.queryKey} onTokenChange={t => this.setState({ currentToken: t }) } subTokenOptions={SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement} readOnly={false} />
                <div className="btn-group" style={{ marginLeft: "10px" }}>
                    {this.renderButton(TemplateTokenMessage.Insert.niceToString(), this.canElement(), token => `@[${token}]`) }
                    {this.renderButton("if", this.canIf(), token => this.props.forHtml ?
                        `<!--@if[${token}]--> <!--@else--> <!--@endif-->` :
                        `@if[${token}] @else @endif`) }
                    {this.renderButton("foreach", this.canForeach(), token => this.props.forHtml ?
                        `<!--@foreach[${token}]--> <!--@endforeach-->` :
                        `@foreach[${token}] @endforeach`) }
                    {this.renderButton("any", this.canElement(), token => this.props.forHtml ?
                        `<!--@any[${token}]--> <!--@notany--> <!--@endany-->` :
                        `@any[${token}] @notany @end`) }
                </div>
            </div>
        );
    }

    renderButton(text: string, canClick: string, buildPattern: (key: string) => string) {
        return <input type="button" disabled={!!canClick} className="btn btn-default btn-sm sf-button"
            title={canClick} value={text}
            onClick={() => this.props.onInsert(buildPattern(this.state.currentToken ? this.state.currentToken.fullKey : "")) }/>;
    }


    canElement(): string {
        let token = this.state.currentToken;

        if (token == null)
            return TemplateTokenMessage.NoColumnSelected.niceToString();

        if (token.type.isCollection)
            return TemplateTokenMessage.YouCannotAddIfBlocksOnCollectionFields.niceToString();

        if (hasAnyOrAll(token))
            return TemplateTokenMessage.YouCannotAddBlocksWithAllOrAny.niceToString();

        return null;
    }


    canIf(): string {
        let token = this.state.currentToken;

        if (token == null)
            return TemplateTokenMessage.NoColumnSelected.niceToString();

        if (token.type.isCollection)
            return TemplateTokenMessage.YouCannotAddIfBlocksOnCollectionFields.niceToString();

        if (hasAnyOrAll(token))
            return TemplateTokenMessage.YouCannotAddBlocksWithAllOrAny.niceToString();

        return null;
    }

    canForeach(): string {

        let token = this.state.currentToken;

        if (token == null)
            return TemplateTokenMessage.NoColumnSelected.niceToString();

        if (token.type.isCollection)
            return TemplateTokenMessage.YouHaveToAddTheElementTokenToUseForeachOnCollectionFields.niceToString();

        if (token.key != "Element" || token.parent == null || !token.parent.type.isCollection)
            return TemplateTokenMessage.YouCanOnlyAddForeachBlocksWithCollectionFields.niceToString();

        if (hasAnyOrAll(token))
            return TemplateTokenMessage.YouCannotAddBlocksWithAllOrAny.niceToString();

        return null;
    }

    canAny() {

        let token = this.state.currentToken;

        if (token == null)
            return TemplateTokenMessage.NoColumnSelected.niceToString();

        if (hasAnyOrAll(token))
            return TemplateTokenMessage.YouCannotAddBlocksWithAllOrAny.niceToString();

        return null;
    }
}





