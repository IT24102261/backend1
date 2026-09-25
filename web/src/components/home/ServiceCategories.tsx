import { Hammer, Paintbrush, Snowflake, Sparkles, Sun, Wrench, Zap, type LucideIcon } from 'lucide-react'

type Service = {
  name: string
  description: string
  image: string
  icon: LucideIcon
}

const featured: Service[] = [
  {
    name: 'Electrical',
    description: 'Switch replacement, lighting and household wiring with category-approved electricians.',
    image: 'https://images.unsplash.com/photo-1621905251189-08b45d6a269e?auto=format&fit=crop&w=700&q=80',
    icon: Zap,
  },
  {
    name: 'Plumbing',
    description: 'Leaks, fittings and bathroom plumbing matched only to approved plumbers.',
    image: 'https://images.unsplash.com/photo-1585704032915-c3400ca199e7?auto=format&fit=crop&w=700&q=80',
    icon: Wrench,
  },
  {
    name: 'Carpentry',
    description: 'Doors, furniture and timber repairs handled by verified carpenters.',
    image: 'https://images.unsplash.com/photo-1504148455328-c376907d081c?auto=format&fit=crop&w=700&q=80',
    icon: Hammer,
  },
]

const more: Service[] = [
  {
    name: 'AC / Refrigeration',
    description: 'Cooling system servicing and diagnosis.',
    image: 'https://images.unsplash.com/photo-1575806980027-9e8310a6e4ab?auto=format&fit=crop&w=700&q=80',
    icon: Snowflake,
  },
  {
    name: 'Solar',
    description: 'Residential solar inspection and technical help.',
    image: 'https://images.unsplash.com/photo-1509391366360-2e959784a276?auto=format&fit=crop&w=700&q=80',
    icon: Sun,
  },
  {
    name: 'Painter',
    description: 'Interior and exterior painting requests.',
    image: 'https://images.unsplash.com/photo-1652829069834-2c05031199c5?auto=format&fit=crop&w=700&q=80',
    icon: Paintbrush,
  },
  {
    name: 'Appliance Repair',
    description: 'Home appliance diagnosis and repair.',
    image: 'https://images.unsplash.com/photo-1775210727386-4c798dfae209?auto=format&fit=crop&w=700&q=80',
    icon: Sparkles,
  },
]

function ServicePortrait({ item, compact }: { item: Service; compact?: boolean }) {
  const Icon = item.icon

  return (
    <article className="flex flex-col items-center">
      <div className="relative">
        <img
          src={item.image}
          alt={`${item.name} work`}
          className={`rounded-full object-cover ring-8 ring-[#1b1e24] ${
            compact ? 'h-44 w-44 sm:h-48 sm:w-48' : 'h-56 w-56 sm:h-60 sm:w-60'
          }`}
        />
        <span className="absolute right-3 bottom-3 flex h-12 w-12 items-center justify-center rounded-full bg-[#c4a574] text-[#111318]">
          <Icon size={18} aria-hidden="true" />
        </span>
      </div>
      <h3 className="landing-serif mt-7 text-2xl">{item.name}</h3>
      <p className="mt-3 max-w-xs text-sm leading-7 text-white/65">{item.description}</p>
    </article>
  )
}

export function ServiceCategories() {
  return (
    <section id="services" className="scroll-mt-24 bg-[#111318] py-24 text-white">
      <div className="mx-auto max-w-6xl px-4 text-center sm:px-6">
        <p className="landing-kicker text-[#c4a574]">What we do</p>
        <h2 className="landing-serif mx-auto mt-4 max-w-3xl text-4xl leading-tight sm:text-5xl">
          Complete home-service matching across verified trades
        </h2>
        <div className="mt-14 grid gap-12 md:grid-cols-3">
          {featured.map((item) => (
            <ServicePortrait key={item.name} item={item} />
          ))}
        </div>
        <div className="mt-16 grid gap-10 sm:grid-cols-2 lg:grid-cols-4">
          {more.map((item) => (
            <ServicePortrait key={item.name} item={item} compact />
          ))}
        </div>
        <a href="#how-it-works" className="landing-btn landing-btn-ghost mt-12">
          View the workflow
        </a>
      </div>
    </section>
  )
}
