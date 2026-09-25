import { Link } from 'react-router-dom'

const photos = [
  'https://images.unsplash.com/photo-1600210492486-724fe5c67fb0?auto=format&fit=crop&w=800&q=80',
  'https://images.unsplash.com/photo-1556912173-46c336c7fd55?auto=format&fit=crop&w=800&q=80',
  'https://images.unsplash.com/photo-1600596542815-ffad4c1539a9?auto=format&fit=crop&w=800&q=80',
  'https://images.unsplash.com/photo-1552321554-5fefe8c9ef14?auto=format&fit=crop&w=800&q=80',
]

export function TrustSection() {
  return (
    <section id="about" className="scroll-mt-24 bg-[#f4efe6] py-24">
      <div className="mx-auto grid max-w-6xl items-center gap-14 px-4 sm:px-6 lg:grid-cols-2">
        <div>
          <p className="landing-kicker text-[#c4a574]">About FixFlow</p>
          <h2 className="landing-serif mt-4 text-4xl leading-tight sm:text-5xl">
            Built on experience.
            <br />
            Focused on people.
          </h2>
          <p className="mt-6 max-w-md text-sm leading-7 text-[#4f4c47] sm:text-base">
            Technicians are approved independently for each category. Exact addresses stay hidden until you confirm a
            booking. AI recommends, then you choose.
          </p>
          <Link to="/register?role=customer" className="landing-btn landing-btn-dark mt-8">
            Learn more
          </Link>
        </div>
        <div className="grid grid-cols-2 gap-3">
          {photos.map((src, index) => (
            <img
              key={src}
              src={src}
              alt="Home interior from a completed service visit"
              className={`h-44 w-full object-cover sm:h-52 ${index % 2 === 1 ? 'mt-8' : ''}`}
            />
          ))}
        </div>
      </div>
    </section>
  )
}
