type StarRatingProps = {
  value: number
  count?: number
  onChange?: (value: number) => void
}

export function StarRating({ value, count, onChange }: StarRatingProps) {
  const rounded = Math.round(value)
  return (
    <div className="flex flex-wrap items-center gap-2">
      <div className="flex items-center gap-1" aria-label={`${value.toFixed(1)} out of 5 stars`}>
        {[1, 2, 3, 4, 5].map((star) => {
          const mark = (
            <span className={`text-xl leading-none ${star <= rounded ? 'text-[#c4a574]' : 'text-[#d9d3c7]'}`}>★</span>
          )
          if (!onChange) return <span key={star}>{mark}</span>
          return (
            <button
              key={star}
              type="button"
              onClick={() => onChange(star)}
              className="cursor-pointer"
              aria-label={`${star} star${star === 1 ? '' : 's'}`}
            >
              {mark}
            </button>
          )
        })}
      </div>
      <span className="text-sm text-[#6d6a64]">
        {count == null ? value.toFixed(1) : count === 0 ? 'No reviews yet' : `${value.toFixed(1)} from ${count} review${count === 1 ? '' : 's'}`}
      </span>
    </div>
  )
}
