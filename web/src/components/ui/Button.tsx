import type { ButtonHTMLAttributes } from 'react'
import { cn } from '../../utils/cn'

export function Button({
  variant = 'primary',
  size = 'md',
  className,
  ...props
}: ButtonHTMLAttributes<HTMLButtonElement> & {
  variant?: 'primary' | 'secondary' | 'danger' | 'ghost'
  size?: 'sm' | 'md'
}) {
  const styles = {
    primary: 'border-transparent bg-[#171717] text-white shadow-sm hover:bg-[#2a2a2a] hover:shadow-md',
    secondary: 'border-[#171717]/15 bg-white text-[#171717] hover:border-[#c4a574] hover:bg-[#f4efe6]',
    danger: 'border-transparent bg-rose-700 text-white hover:bg-rose-600',
    ghost: 'border-transparent text-[#6d6a64] hover:bg-[#f4efe6] hover:text-[#171717]',
  }
  const sizes = {
    sm: 'px-3 py-1.5 text-xs',
    md: 'px-4 py-2.5 text-sm',
  }

  return (
    <button
      type="button"
      className={cn(
        'inline-flex items-center justify-center gap-2 rounded-xl border font-medium transition duration-150 hover:-translate-y-px disabled:pointer-events-none disabled:translate-y-0 disabled:opacity-50',
        sizes[size],
        styles[variant],
        className,
      )}
      {...props}
    />
  )
}
