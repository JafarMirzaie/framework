﻿import * as React from 'react'
import { DropdownButton, MenuItem } from 'react-bootstrap'
import { FormGroup, FormControlStatic, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater } from '../../../../Framework/Signum.React/Scripts/Lines'
import { ModifiableEntity, OperationSymbol, JavascriptMessage, is } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { FindOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { getQueryNiceName, PropertyRoute, getTypeInfo } from '../../../../Framework/Signum.React/Scripts/Reflection'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import * as Operations from '../../../../Framework/Signum.React/Scripts/Operations'
import * as EntityOperations from '../../../../Framework/Signum.React/Scripts/Operations/EntityOperations'
import { BaseNode } from './Nodes'
import { DesignerContext, DesignerNode } from './NodeUtils'
import * as NodeUtils from './NodeUtils'
import * as DynamicViewClient from '../DynamicViewClient'
import { DynamicViewInspector, CollapsableTypeHelp } from './Designer'
import { DynamicViewTree } from './DynamicViewTree'
import { AuthInfo } from './AuthInfo'
import { DynamicViewEntity, DynamicViewOperation, DynamicViewMessage } from '../Signum.Entities.Dynamic'

require("!style!css!./DynamicView.css");

export interface DynamicViewComponentProps {
    ctx: TypeContext<ModifiableEntity>;
    initialDynamicView: DynamicViewEntity;
}

export interface DynamicViewComponentState {
    isDesignerOpen: boolean;
    rootNode: BaseNode;
    selectedNode: DesignerNode<BaseNode>;
    dynamicView: DynamicViewEntity;
}

export default class DynamicViewComponent extends React.Component<DynamicViewComponentProps, DynamicViewComponentState>{

    constructor(props: DynamicViewComponentProps) {
        super(props);
        
        const rootNode = JSON.parse(props.initialDynamicView.viewContent!) as BaseNode;;
        this.state = {
            dynamicView: props.initialDynamicView,
            isDesignerOpen: false,
            rootNode: rootNode,
            selectedNode: this.getZeroNode().createChild(rootNode)
        };
    }

    getZeroNode() {

        const typeName = this.props.ctx.value.Type;

        var context = {
            onClose: this.handleClose,
            refreshView: () => { this.changeState(s => s.selectedNode = s.selectedNode.reCreateNode()); },
            getSelectedNode: () => this.state.isDesignerOpen ? this.state.selectedNode : undefined,
            setSelectedNode: (newNode) => this.changeState(s => s.selectedNode = newNode)
        } as DesignerContext;
        
        return DesignerNode.zero(context, typeName);
    }

    handleReload = (dynamicView: DynamicViewEntity) => {
        
        this.changeState(s => {
            s.dynamicView = dynamicView;
            s.rootNode = JSON.parse(dynamicView.viewContent!) as BaseNode;
            s.selectedNode = this.getZeroNode().createChild(s.rootNode);
        });
    }

    handleOpen= () => {
        this.changeState(s => s.isDesignerOpen = true);
    }

    handleClose = () => {
        this.changeState(s => s.isDesignerOpen = false);
    }

    render() {

        var rootNode = this.getZeroNode().createChild(this.state.rootNode);

        return (<div className="design-main">
            <div className={classes("design-left", this.state.isDesignerOpen && "open")}>
                {!this.state.isDesignerOpen ?
                    <i className="fa fa-pencil-square-o design-open-icon" aria-hidden="true" onClick={this.handleOpen}></i> :
                    <DynamicViewDesigner
                        rootNode={rootNode}
                        dynamicView={this.state.dynamicView}
                        onReload={this.handleReload}
                        onLoseChanges={this.handleLoseChanges}
                        typeName={this.props.ctx.value.Type} />
                }
            </div>
            <div className={classes("design-content", this.state.isDesignerOpen && "open")}>
                {NodeUtils.render(rootNode, this.props.ctx)}
            </div>
        </div>);
    }

    handleLoseChanges = () => {
        const node = JSON.stringify(this.state.rootNode);

        if (this.state.dynamicView.isNew || node != this.state.dynamicView.viewContent) {
            return confirm(JavascriptMessage.loseCurrentChanges.niceToString());
        }

        return true;
    }
}

interface DynamicViewDesignerProps {
    rootNode: DesignerNode<BaseNode>;
    dynamicView: DynamicViewEntity;
    onLoseChanges: () => boolean;
    onReload: (dynamicView: DynamicViewEntity) => void;
    typeName: string;
}


class DynamicViewDesigner extends React.Component<DynamicViewDesignerProps, { viewNames?: string[]; }>{

    constructor(props: DynamicViewDesignerProps) {
        super(props);
        this.state = {};
    }

    render() {
        var dv = this.props.dynamicView;
        var ctx = TypeContext.root(DynamicViewEntity, dv);

        return (
            <div className="form-vertical code-container">
                <button type="button" className="close" aria-label="Close" onClick={this.props.rootNode.context.onClose}><span aria-hidden="true">×</span></button>
                <h3>
                    <small>{Navigator.getTypeTitle(this.props.dynamicView, undefined)}</small>
                </h3>
                <ValueLine ctx={ctx.subCtx(e => e.viewName)} formGroupStyle="SrOnly" placeholderLabels={true} />
                {this.renderButtonBar()}
                <DynamicViewTree rootNode={this.props.rootNode} />
                <DynamicViewInspector selectedNode={this.props.rootNode.context.getSelectedNode()} />
                <CollapsableTypeHelp initialTypeName={dv.entityType!.cleanName} />
            </div>
        );
    }



    reload(entity: DynamicViewEntity) {
        this.changeState(s => s.viewNames = undefined);
        this.props.onReload(entity);
    }

    handleSave = () => {

        this.props.dynamicView.viewContent = JSON.stringify(this.props.rootNode.node);

        Operations.API.executeEntity(this.props.dynamicView, DynamicViewOperation.Save)
            .then(pack => { this.reload(pack.entity); return EntityOperations.notifySuccess(); })
            .done();
    }

    handleCreate = () => {

        if (!this.props.onLoseChanges())
            return;

        DynamicViewClient.createDefaultDynamicView(this.props.typeName)
            .then(entity => { this.reload(entity); return EntityOperations.notifySuccess(); })
            .done();
    }

    handleClone = () => {

        if (!this.props.onLoseChanges())
            return;

        Operations.API.constructFromEntity(this.props.dynamicView, DynamicViewOperation.Clone)
            .then(pack => { this.reload(pack.entity); return EntityOperations.notifySuccess(); })
            .done();
    }
    
    handleChangeView = (viewName: string) => {
        if (!this.props.onLoseChanges())
            return;

        DynamicViewClient.API.getDynamicView(this.props.typeName, viewName)
            .then(entity => { this.reload(entity!); })
            .done();
    }

    handleOnToggle = (isOpen: boolean) => {
        if (isOpen && !this.state.viewNames)
            DynamicViewClient.API.getDynamicViewNames(this.props.typeName)
                .then(viewNames => this.changeState(s => s.viewNames = viewNames))
                .done();
    }


    renderButtonBar() {

        var operations = Operations.operationInfos(getTypeInfo(DynamicViewEntity)).toObject(a => a.key);

        return (
            <div className="btn-group btn-group-sm" role="group" style={{ marginBottom: "5px"}}>
                {operations[DynamicViewOperation.Save.key] && <button type="button" className="btn btn-primary" onClick={this.handleSave}>{operations[DynamicViewOperation.Save.key].niceName}</button>}

                <DropdownButton title=" … " id="bg-nested-dropdown" onToggle={this.handleOnToggle} bsSize="sm">
                    {operations[DynamicViewOperation.Create.key] && <MenuItem eventKey="create" onSelect={this.handleCreate}>{operations[DynamicViewOperation.Create.key].niceName}</MenuItem>}
                    {operations[DynamicViewOperation.Clone.key] && !this.props.dynamicView.isNew && <MenuItem eventKey="clone" onSelect={this.handleClone}>{operations[DynamicViewOperation.Clone.key].niceName}</MenuItem>}
                    {this.state.viewNames && this.state.viewNames.length > 0 && <MenuItem divider={true} />}
                    {this.state.viewNames && this.state.viewNames.map(vn => <MenuItem key={vn}
                        eventKey={"view-" + vn}
                        className={classes("sf-dynamic-view", vn == this.props.dynamicView.viewName && "active")}
                        onSelect={() => this.handleChangeView(vn)}>
                        {vn}
                    </MenuItem>)}
                </DropdownButton>
            </div >);
    }
}

