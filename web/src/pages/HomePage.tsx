import { LandingNavbar } from '../components/home/Navbar'
import { Hero } from '../components/home/Hero'
import { ServiceCategories } from '../components/home/ServiceCategories'
import { StatsSection } from '../components/home/StatsSection'
import { TrustSection } from '../components/home/TrustSection'
import { HowItWorks } from '../components/home/HowItWorks'
import { UserRolesSection } from '../components/home/UserRolesSection'
import { CtaSection } from '../components/home/CtaSection'
import { LandingFooter } from '../components/home/Footer'
import '../components/home/home.css'

export function HomePage() {
  return (
    <div className="landing-page">
      <LandingNavbar />
      <Hero />
      <ServiceCategories />
      <StatsSection />
      <TrustSection />
      <HowItWorks />
      <UserRolesSection />
      <CtaSection />
      <LandingFooter />
    </div>
  )
}
