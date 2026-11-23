import { useId } from "react";
import type { InputHTMLAttributes, ReactNode, Ref, SelectHTMLAttributes, TextareaHTMLAttributes } from "react";
import { cx } from "./cx";
import { Field } from "./Field";
import fieldStyles from "./Field.module.css";

type Common = {
  label: ReactNode;
  hint?: ReactNode;
  error?: string;
  fieldClassName?: string;
};

export type TextInputProps = Common & Omit<InputHTMLAttributes<HTMLInputElement>, "id"> & { ref?: Ref<HTMLInputElement> };

export function TextInput({ label, hint, error, fieldClassName, required, className, ...rest }: TextInputProps) {
  return (
    <Field label={label} hint={hint} error={error} required={required} className={fieldClassName}>
      {(control) => <input className={cx(fieldStyles.control, className)} {...control} {...rest} />}
    </Field>
  );
}

export type TextAreaProps = Common & Omit<TextareaHTMLAttributes<HTMLTextAreaElement>, "id"> & { ref?: Ref<HTMLTextAreaElement> };

export function TextArea({ label, hint, error, fieldClassName, required, className, ...rest }: TextAreaProps) {
  return (
    <Field label={label} hint={hint} error={error} required={required} className={fieldClassName}>
      {(control) => <textarea className={cx(fieldStyles.control, className)} {...control} {...rest} />}
    </Field>
  );
}

export type SelectProps = Common &
  Omit<SelectHTMLAttributes<HTMLSelectElement>, "id"> & { ref?: Ref<HTMLSelectElement>; placeholder?: string };

export function Select({ label, hint, error, fieldClassName, required, className, placeholder, children, ...rest }: SelectProps) {
  return (
    <Field label={label} hint={hint} error={error} required={required} className={fieldClassName}>
      {(control) => (
        <select className={cx(fieldStyles.control, className)} {...control} {...rest}>
          {placeholder !== undefined ? <option value="">{placeholder}</option> : null}
          {children}
        </select>
      )}
    </Field>
  );
}

export type CheckboxProps = Omit<InputHTMLAttributes<HTMLInputElement>, "id" | "type"> & {
  label: ReactNode;
  hint?: ReactNode;
  error?: string;
  ref?: Ref<HTMLInputElement>;
};

export function Checkbox({ label, hint, error, className, ...rest }: CheckboxProps) {
  const id = useId();
  const hintId = hint ? `${id}-hint` : undefined;
  const errorId = error ? `${id}-error` : undefined;
  const describedBy = [hintId, errorId].filter(Boolean).join(" ") || undefined;

  return (
    <div className={fieldStyles.field}>
      <label htmlFor={id} className={fieldStyles.checkbox}>
        <input id={id} type="checkbox" className={className} aria-describedby={describedBy} aria-invalid={error ? true : undefined} {...rest} />
        <span>{label}</span>
      </label>
      {hint ? (
        <p id={hintId} className={fieldStyles.hint}>
          {hint}
        </p>
      ) : null}
      {error ? (
        <p id={errorId} className={fieldStyles.error}>
          {error}
        </p>
      ) : null}
    </div>
  );
}
