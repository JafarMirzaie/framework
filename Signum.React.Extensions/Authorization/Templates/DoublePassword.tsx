import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { UserEntity, UserState, LoginAuthMessage } from '../Signum.Entities.Authorization'
import { FormGroup } from '@framework/Lines/FormGroup'
import { TypeContext } from '@framework/TypeContext'
import { classes } from '../../../Signum.React/Scripts/Globals'

export function DoublePassword(p: { ctx: TypeContext<string>, isNew: boolean, mandatory: boolean }) {

  const [withPassword, setWithPassword] = React.useState(p.isNew);
  var newPass = React.useRef<HTMLInputElement>(null);
  var newPass2 = React.useRef<HTMLInputElement>(null);



  function handlePasswordBlur(e: React.SyntheticEvent<any>) {
    const ctx = p.ctx;

    if (newPass.current!.value && newPass2.current!.value) {
      if (newPass.current!.value != newPass2.current!.value) {
        ctx.error = LoginAuthMessage.PasswordsAreDifferent.niceToString()
      }
      else {
        ctx.error = undefined;
        ctx.value = newPass.current!.value;
      }
    }

    ctx.frame!.revalidate();
  }

  if (!withPassword) {
    return (
      <FormGroup labelText={LoginAuthMessage.NewPassword.niceToString()} ctx={p.ctx}>
        <a className="btn btn-light btn-sm" onClick={() => setWithPassword(true)}>
          <FontAwesomeIcon icon="key" /> {LoginAuthMessage.ChangePassword.niceToString()}
        </a>
      </FormGroup>
    );
  }

  return (
    <div>
      <FormGroup ctx={p.ctx} labelText={LoginAuthMessage.NewPassword.niceToString()}>
        <input type="password" ref={newPass} autoComplete="off" className={classes(p.ctx.formControlClass, p.mandatory && !newPass.current?.value ? "sf-mandatory" : null)} onBlur={handlePasswordBlur} />
      </FormGroup>
      <FormGroup ctx={p.ctx} labelText={LoginAuthMessage.ConfirmNewPassword.niceToString()}>
        <input type="password" ref={newPass2} autoComplete="off" className={classes(p.ctx.formControlClass, p.mandatory && !newPass2.current?.value  ? "sf-mandatory" : null)} onBlur={handlePasswordBlur} />
      </FormGroup>
    </div>
  );
}
