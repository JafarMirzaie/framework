﻿import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { ifError } from '@framework/Globals'
import * as Finder from '@framework/Finder'
import { ValidationError, AbortableRequest } from '@framework/Services'
import { Lite } from '@framework/Signum.Entities'
import { QueryDescription, SubTokensOptions, QueryToken } from '@framework/FindOptions'
import { TypeContext } from '@framework/TypeContext'
import { SearchMessage, JavascriptMessage } from '@framework/Signum.Entities'
import { PropertyRoute, getQueryNiceName, getTypeInfo, ReadonlyBinding, GraphExplorer }  from '@framework/Reflection'
import * as Navigator from '@framework/Navigator'
import FilterBuilder from '@framework/SearchControl/FilterBuilder'
import ValidationErrors from '@framework/Frames/ValidationErrors'
import { ChartRequestModel, ChartMessage, UserChartEntity } from '../Signum.Entities.Chart'
import * as ChartClient from '../ChartClient'
import ChartBuilder from './ChartBuilder'
import ChartTableComponent from './ChartTable'
import ChartRenderer from './ChartRenderer'
import "../Chart.css"
import "@framework/SearchControl/Search.css"
import { Tab, UncontrolledTabs } from '@framework/Components/Tabs';
import { ChartScript } from '../ChartClient';


interface ChartRequestViewProps {
    chartRequest?: ChartRequestModel;
    userChart?: Lite<UserChartEntity>;
    onChange: (newChartRequest: ChartRequestModel, userChart?: Lite<UserChartEntity>) => void;
    title?: string;
}

interface ChartRequestViewState {
    queryDescription?: QueryDescription;
    lastChartRequest?: ChartRequestModel;
    chartResult?: ChartClient.API.ExecuteChartResult;
}

export default class ChartRequestView extends React.Component<ChartRequestViewProps, ChartRequestViewState> {

    lastToken: QueryToken | undefined;

    constructor(props: ChartRequestViewProps) {
        super(props);
        this.state = { };
   
    }

    componentWillMount() {
        this.loadQueryDescription(this.props);
    }

    componentWillReceiveProps(nextProps: ChartRequestViewProps) {

        var oldPath = this.props.chartRequest && ChartClient.Encoder.chartPath(this.props.chartRequest, this.props.userChart);
        var newPath = nextProps.chartRequest && ChartClient.Encoder.chartPath(nextProps.chartRequest, nextProps.userChart);

        if (oldPath == newPath)
            return;
        
        this.setState({ chartResult: undefined, lastChartRequest: undefined });
        this.loadQueryDescription(nextProps);
    }

    loadQueryDescription(props: ChartRequestViewProps) {
        if (props.chartRequest) {
            Finder.getQueryDescription(props.chartRequest.queryKey).then(qd => {
                this.setState({ queryDescription: qd });
            }).done();
        }
    }

    handleTokenChange = () => {
        this.removeObsoleteOrders();
    }

    handleInvalidate = () => {
        this.removeObsoleteOrders();
        this.setState({ chartResult: undefined, lastChartRequest: undefined });
    }

    removeObsoleteOrders() {
        var cr = this.props.chartRequest;
        if (cr && cr.groupResults) {
            var oldOrders = cr.orderOptions.filter(o =>
                o.token.queryTokenType != "Aggregate" &&
                !cr!.columns.some(mle2 => !!mle2.element.token && !!mle2.element.token.token && mle2.element.token.token.fullKey == o.token.fullKey));

            oldOrders.forEach(o => cr!.orderOptions.remove(o));
        }
    }

    handleOnRedraw = () => {
        this.forceUpdate();
        this.props.onChange(this.props.chartRequest!, this.props.userChart);
    }
    
    componentWillUnmount() {
        this.abortableQuery.abort();
    }

    abortableQuery = new AbortableRequest<{ cr: ChartRequestModel; cs: ChartScript }, ChartClient.API.ExecuteChartResult>((signal, request) => ChartClient.API.executeChart(request.cr, request.cs, signal))

    handleOnDrawClick = () => {

        this.setState({ chartResult: undefined, lastChartRequest: undefined });

        var cr = this.props.chartRequest!;

        Navigator.API.validateEntity(cr)
            .then(() => {
                ChartClient.getChartScript(cr.chartScript)
                    .then(cs => this.abortableQuery.getData({ cr, cs }))
                    .then(
                        rt => {
                            this.setState({ chartResult: rt, lastChartRequest: JSON.parse(JSON.stringify(this.props.chartRequest)) });
                            this.props.onChange(cr, this.props.userChart);
                        }).done();
            }, ifError(ValidationError, e => {
                GraphExplorer.setModelState(cr, e.modelState, "entity");
                this.forceUpdate();
            })).done();
    }

    handleOnFullScreen = (e: React.MouseEvent<any>) => {
        e.preventDefault();
        Navigator.history.push(ChartClient.Encoder.chartPath(this.props.chartRequest!));
    }

    render() {
        const cr = this.props.chartRequest;
        const qd = this.state.queryDescription;
        const s = this.state;

        if (cr == undefined || qd == undefined)
            return null;

        const tc = new TypeContext<ChartRequestModel>(undefined, undefined, PropertyRoute.root(getTypeInfo(cr.Type)), new ReadonlyBinding(this.props.chartRequest!, ""));

        return (
            <div>
                <h2>
                    <span className="sf-entity-title">{getQueryNiceName(cr.queryKey) }</span>&nbsp;
                    <a className ="sf-popup-fullscreen" href="#" onClick={this.handleOnFullScreen}>
                        <FontAwesomeIcon icon="external-link-alt" />
                    </a>
                </h2 >
                <ValidationErrors entity={cr}/>
                <div className="sf-chart-control SF-control-container" >
                    <div>
                        <FilterBuilder filterOptions={cr.filterOptions} queryDescription={this.state.queryDescription!}
                            subTokensOptions={SubTokensOptions.CanAggregate | SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement}
                            lastToken={this.lastToken} onTokenChanged={t => this.lastToken = t} />

                    </div>
                    <div className="SF-control-container">
                        <ChartBuilder queryKey={cr.queryKey} ctx={tc}
                            onInvalidate={this.handleInvalidate}
                            onRedraw={this.handleOnRedraw}
                            onTokenChange={this.handleTokenChange}
                            />
                    </div >
                    <div className="sf-query-button-bar btn-toolbar">
                        <button type="submit" className="sf-query-button sf-chart-draw btn btn-primary" onClick={this.handleOnDrawClick}>{ChartMessage.DrawChart.niceToString()}</button>
                        {ChartClient.ButtonBarChart.getButtonBarElements({ chartRequest: cr, chartRequestView: this }).map((a, i) => React.cloneElement(a, { key: i }))}
                        <button className="btn btn-light" onMouseUp={this.handleExplore} ><FontAwesomeIcon icon="search" /> &nbsp; {SearchMessage.Explore.niceToString()}</button>
                    </div>
                    <br />
                    <div className="sf-scroll-table-container" >
                        {!s.chartResult || !s.lastChartRequest ? JavascriptMessage.searchForResults.niceToString() :
                            <UncontrolledTabs id="chartResultTabs">
                                <Tab eventKey="chart" title={ChartMessage.Chart.niceToString()}>
                                    <ChartRenderer chartRequest={cr} lastChartRequest={s.lastChartRequest} data={s.chartResult.chartTable} />
                                </Tab>

                                <Tab eventKey="data" title={<span>{ChartMessage.Data.niceToString()} ({(s.chartResult.resultTable.rows.length)})</span> as any}>
                                    <ChartTableComponent chartRequest={cr} lastChartRequest={s.lastChartRequest} resultTable={s.chartResult.resultTable} onRedraw={this.handleOnDrawClick} />
                                </Tab>
                            </UncontrolledTabs>
                        }
                    </div>
                </div>
            </div>
        );
    }


    handleExplore = (e: React.MouseEvent<any>) => {
        const cr = this.props.chartRequest!;

        var path = Finder.findOptionsPath({
            queryName: cr.queryKey,
            filterOptions: Finder.toFilterOptions(cr.filterOptions),
        });

        Navigator.pushOrOpenInTab(path, e);
    }
}







