import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { Link } from 'react-router-dom'
import { Entity, parseLite, getToString, JavascriptMessage, EntityPack } from '@framework/Signum.Entities'
import * as Navigator from '@framework/Navigator'
import { DashboardEntity } from '../Signum.Entities.Dashboard'
import DashboardView from './DashboardView'
import { RouteComponentProps } from "react-router";
import "../Dashboard.css"
import { useAPI } from '@framework/Hooks'
import { QueryString } from '@framework/QueryString'
import { translated } from '../../Translation/TranslatedInstanceTools'

interface DashboardPageProps extends RouteComponentProps<{ dashboardId: string }> {

}

function getQueryEntity(props: DashboardPageProps): string {
  return QueryString.parse(props.location.search).entity as string;
}

export default function DashboardPage(p: DashboardPageProps) {

  const dashboard = useAPI(signal => Navigator.API.fetchEntity(DashboardEntity, p.match.params.dashboardId), [p.match.params.dashboardId]);

  var entityKey = getQueryEntity(p);

  const entity = useAPI(signal => entityKey ? Navigator.API.fetchAndForget(parseLite(entityKey)) : Promise.resolve(null), [p.match.params.dashboardId]);

  const rtl = React.useMemo(() => document.body.classList.contains("rtl"), []);

  return (
    <div>
      {!dashboard ? <h2 className="display-5">{JavascriptMessage.loading.niceToString()}</h2> :
        <div className="sf-show-hover">
          {!Navigator.isReadOnly(DashboardEntity) &&
            <Link className="sf-hide float-right flip mt-3" style={{ textDecoration: "none" }} to={Navigator.navigateRoute(dashboard)}><FontAwesomeIcon icon="edit" />&nbsp;Edit</Link>
          }
          <h2 className="display-5">{translated(dashboard, d => d.displayName)}</h2>
        </div>}

      {entityKey &&
        <div style={rtl ? { float: "right", textAlign: "right" } : undefined}>
          {!entity ? <h3>{JavascriptMessage.loading.niceToString()}</h3> :
            <h3>
              {Navigator.isViewable({ entity: entity, canExecute: {} } as EntityPack<Entity>) ?
                <Link className="display-6" to={Navigator.navigateRoute(entity)}>{getToString(entity)}</Link> :
                <span className="display-6">{getToString(entity)}</span>
              }
              &nbsp;
            <small className="sf-type-nice-name">{Navigator.getTypeTitle(entity, undefined)}</small>
            </h3>
          }
        </div>
      }

      {dashboard && (!entityKey || entity) && <DashboardView dashboard={dashboard} entity={entity || undefined} deps={entity?.ticks} />}
    </div>
  );
}
