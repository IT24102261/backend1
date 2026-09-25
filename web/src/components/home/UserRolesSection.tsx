import { Link } from 'react-router-dom'

const customer = [
  'Create service requests and upload photos',
  'Receive quotations from eligible technicians',
  'Compare recommendation explanations',
  'Confirm the booking and leave a review',
]

const technician = [
  'Apply for a service category with documents',
  'Receive invitations only in your approved trade',
  'Submit quotations with a proposed arrival',
  'Update the job until the customer confirms',
]

export function UserRolesSection() {
  return (
    <section id="for-technicians" className="scroll-mt-24 bg-[#f4efe6] py-24">
      <div className="mx-auto grid max-w-6xl gap-10 px-4 sm:px-6 lg:grid-cols-2">
        <article className="bg-white p-10">
          <p className="landing-kicker text-[#c4a574]">Customers</p>
          <h2 className="landing-serif mt-4 text-4xl">For homeowners</h2>
          <ul className="mt-8 space-y-4 text-sm leading-7 text-[#4f4c47]">
            {customer.map((item) => (
              <li key={item}>{item}</li>
            ))}
          </ul>
          <Link to="/register?role=customer" className="landing-btn landing-btn-dark mt-10">
            Get started as customer
          </Link>
        </article>
        <article className="bg-[#111318] p-10 text-white">
          <p className="landing-kicker text-[#c4a574]">Technicians</p>
          <h2 className="landing-serif mt-4 text-4xl">For tradespeople</h2>
          <ul className="mt-8 space-y-4 text-sm leading-7 text-white/70">
            {technician.map((item) => (
              <li key={item}>{item}</li>
            ))}
          </ul>
          <Link to="/register?role=technician" className="landing-btn landing-btn-ghost mt-10">
            Join as technician
          </Link>
        </article>
      </div>
    </section>
  )
}
